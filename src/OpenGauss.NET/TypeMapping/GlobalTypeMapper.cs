using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.Internal.TypeMapping;
using OpenGauss.NET.NameTranslation;
using OpenGauss.NET.Types;
using static OpenGauss.NET.Util.Statics;

namespace OpenGauss.NET.TypeMapping
{
    sealed class GlobalTypeMapper : TypeMapperBase
    {
        public static GlobalTypeMapper Instance { get; }

        internal List<TypeHandlerResolverFactory> ResolverFactories { get; } = new();
        public ConcurrentDictionary<string, IUserTypeMapping> UserTypeMappings { get; } = new();

        readonly ConcurrentDictionary<Type, TypeMappingInfo> _mappingsByClrType = new();

        /// <summary>
        /// A counter that is incremented whenever a global mapping change occurs.
        /// Used to invalidate bound type mappers.
        /// </summary>
        internal int ChangeCounter => _changeCounter;

        internal ReaderWriterLockSlim Lock { get; }
            = new(LockRecursionPolicy.SupportsRecursion);

        int _changeCounter;

        static GlobalTypeMapper()
            => Instance = new GlobalTypeMapper();

        GlobalTypeMapper() : base(new OpenGaussSnakeCaseNameTranslator())
            => Reset();

        #region Mapping management

        public override IOpenGaussTypeMapper MapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(TEnum), nameTranslator);

            Lock.EnterWriteLock();
            try
            {
                UserTypeMappings[pgName] = new UserEnumTypeMapping<TEnum>(pgName, nameTranslator);
                RecordChange();
                return this;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override bool UnmapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(TEnum), nameTranslator);

            Lock.EnterWriteLock();
            try
            {
                if (UserTypeMappings.TryRemove(pgName, out _))
                {
                    RecordChange();
                    return true;
                }

                return false;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override IOpenGaussTypeMapper MapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(T), nameTranslator);

            Lock.EnterWriteLock();
            try
            {
                UserTypeMappings[pgName] = new UserCompositeTypeMapping<T>(pgName, nameTranslator);
                RecordChange();
                return this;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override IOpenGaussTypeMapper MapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(clrType, nameTranslator);

            Lock.EnterWriteLock();
            try
            {
                UserTypeMappings[pgName] =
                    (IUserTypeMapping)Activator.CreateInstance(typeof(UserCompositeTypeMapping<>).MakeGenericType(clrType),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new object[] { clrType, nameTranslator }, null)!;

                RecordChange();

                return this;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override bool UnmapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
            => UnmapComposite(typeof(T), pgName, nameTranslator);

        public override bool UnmapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(clrType, nameTranslator);

            Lock.EnterWriteLock();
            try
            {
                if (UserTypeMappings.TryRemove(pgName, out _))
                {
                    RecordChange();
                    return true;
                }

                return false;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override void AddTypeResolverFactory(TypeHandlerResolverFactory resolverFactory)
        {
            Lock.EnterWriteLock();
            try
            {
                // Since EFCore.PG plugins (and possibly other users) repeatedly call OpenGaussConnection.GlobalTypeMapped.UseNodaTime,
                // we replace an existing resolver of the same CLR type.
                var type = resolverFactory.GetType();

                if (ResolverFactories[0].GetType() == type)
                    ResolverFactories[0] = resolverFactory;
                else
                {
                    for (var i = 0; i < ResolverFactories.Count; i++)
                        if (ResolverFactories[i].GetType() == type)
                            ResolverFactories.RemoveAt(i);

                    ResolverFactories.Insert(0, resolverFactory);
                }

                RecordChange();
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public override void Reset()
        {
            Lock.EnterWriteLock();
            try
            {
                ResolverFactories.Clear();
                ResolverFactories.Add(new BuiltInTypeHandlerResolverFactory());

                UserTypeMappings.Clear();

                RecordChange();
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        internal void RecordChange()
        {
            _mappingsByClrType.Clear();
            Interlocked.Increment(ref _changeCounter);
        }

        #endregion Mapping management

        #region OpenGaussDbType/DbType inference for OpenGaussParameter

        [RequiresUnreferencedCodeAttribute("ToOpenGaussDbType uses interface-based reflection and isn't trimming-safe")]
        internal bool TryResolveMappingByValue(object value, [NotNullWhen(true)] out TypeMappingInfo? typeMapping)
        {
            Lock.EnterReadLock();
            try
            {
                // We resolve as follows:
                // 1. Cached by-type lookup (fast path). This will work for almost all types after the very first resolution.
                // 2. Value-dependent type lookup (e.g. DateTime by Kind) via the resolvers. This includes complex types (e.g. array/range
                //    over DateTime), and the results cannot be cached.
                // 3. Uncached by-type lookup (for the very first resolution of a given type)

                var type = value.GetType();
                if (_mappingsByClrType.TryGetValue(type, out typeMapping))
                    return true;

                foreach (var resolverFactory in ResolverFactories)
                    if ((typeMapping = resolverFactory.GetMappingByValueDependentValue(value)) is not null)
                        return true;

                return TryResolveMappingByClrType(value.GetType(), out typeMapping);
            }
            finally
            {
                Lock.ExitReadLock();
            }

            bool TryResolveMappingByClrType(Type clrType, [NotNullWhen(true)] out TypeMappingInfo? typeMapping)
            {
                if (_mappingsByClrType.TryGetValue(clrType, out typeMapping))
                    return true;

                foreach (var resolverFactory in ResolverFactories)
                {
                    if ((typeMapping = resolverFactory.GetMappingByClrType(clrType)) is not null)
                    {
                        _mappingsByClrType[clrType] = typeMapping;
                        return true;
                    }
                }

                if (clrType.IsArray)
                {
                    if (TryResolveMappingByClrType(clrType.GetElementType()!, out var elementMapping))
                    {
                        _mappingsByClrType[clrType] = typeMapping = new(
                            OpenGaussDbType.Array | elementMapping.OpenGaussDbType,
                            elementMapping.DataTypeName + "[]");
                        return true;
                    }

                    typeMapping = null;
                    return false;
                }

                var typeInfo = clrType.GetTypeInfo();

                var ilist = typeInfo.ImplementedInterfaces.FirstOrDefault(x =>
                    x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
                if (ilist != null)
                {
                    if (TryResolveMappingByClrType(ilist.GetGenericArguments()[0], out var elementMapping))
                    {
                        _mappingsByClrType[clrType] = typeMapping = new(
                            OpenGaussDbType.Array | elementMapping.OpenGaussDbType,
                            elementMapping.DataTypeName + "[]");
                        return true;
                    }

                    typeMapping = null;
                    return false;
                }

                if (typeInfo.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(OpenGaussRange<>))
                {
                    if (TryResolveMappingByClrType(clrType.GetGenericArguments()[0], out var elementMapping))
                    {
                        _mappingsByClrType[clrType] = typeMapping = new(
                            OpenGaussDbType.Range | elementMapping.OpenGaussDbType,
                            dataTypeName: null);
                        return true;
                    }

                    typeMapping = null;
                    return false;
                }

                typeMapping = null;
                return false;
            }
        }

        #endregion OpenGaussDbType/DbType inference for OpenGaussParameter

        #region Static translation tables

        public static string? OpenGaussDbTypeToDataTypeName(OpenGaussDbType opengaussDbType)
            => opengaussDbType switch
            {
                // Numeric types
                OpenGaussDbType.Smallint => "smallint",
                OpenGaussDbType.Integer  => "integer",
                OpenGaussDbType.Bigint   => "bigint",
                OpenGaussDbType.Real     => "real",
                OpenGaussDbType.Double   => "double precision",
                OpenGaussDbType.Numeric  => "numeric",
                OpenGaussDbType.Money    => "money",

                // Text types
                OpenGaussDbType.Text      => "text",
                OpenGaussDbType.Xml       => "xml",
                OpenGaussDbType.Varchar   => "character varying",
                OpenGaussDbType.Char      => "character",
                OpenGaussDbType.Name      => "name",
                OpenGaussDbType.Refcursor => "refcursor",
                OpenGaussDbType.Citext    => "citext",
                OpenGaussDbType.Jsonb     => "jsonb",
                OpenGaussDbType.Json      => "json",
                OpenGaussDbType.JsonPath  => "jsonpath",

                // Date/time types
                OpenGaussDbType.Timestamp   => "timestamp without time zone",
                OpenGaussDbType.TimestampTz => "timestamp with time zone",
                OpenGaussDbType.Date        => "date",
                OpenGaussDbType.Time        => "time without time zone",
                OpenGaussDbType.TimeTz      => "time with time zone",
                OpenGaussDbType.Interval    => "interval",

                // Network types
                OpenGaussDbType.Cidr     => "cidr",
                OpenGaussDbType.Inet     => "inet",
                OpenGaussDbType.MacAddr  => "macaddr",
                OpenGaussDbType.MacAddr8 => "macaddr8",

                // Full-text search types
                OpenGaussDbType.TsQuery   => "tsquery",
                OpenGaussDbType.TsVector  => "tsvector",

                // Geometry types
                OpenGaussDbType.Box     => "box",
                OpenGaussDbType.Circle  => "circle",
                OpenGaussDbType.Line    => "line",
                OpenGaussDbType.LSeg    => "lseg",
                OpenGaussDbType.Path    => "path",
                OpenGaussDbType.Point   => "point",
                OpenGaussDbType.Polygon => "polygon",

                // LTree types
                OpenGaussDbType.LQuery    => "lquery",
                OpenGaussDbType.LTree     => "ltree",
                OpenGaussDbType.LTxtQuery => "ltxtquery",

                // UInt types
                OpenGaussDbType.Oid       => "oid",
                OpenGaussDbType.Xid       => "xid",
                OpenGaussDbType.Xid8      => "xid8",
                OpenGaussDbType.Cid       => "cid",
                OpenGaussDbType.Regtype   => "regtype",
                OpenGaussDbType.Regconfig => "regconfig",

                // Misc types
                OpenGaussDbType.Boolean => "bool",
                OpenGaussDbType.Bytea   => "bytea",
                OpenGaussDbType.Uuid    => "uuid",
                OpenGaussDbType.Varbit  => "bit varying",
                OpenGaussDbType.Bit     => "bit",
                OpenGaussDbType.Hstore  => "hstore",

                OpenGaussDbType.Geometry  => "geometry",
                OpenGaussDbType.Geography => "geography",

                // Built-in range types
                OpenGaussDbType.IntegerRange     => "int4range",
                OpenGaussDbType.BigIntRange      => "int8range",
                OpenGaussDbType.NumericRange     => "numrange",
                OpenGaussDbType.TimestampRange   => "tsrange",
                OpenGaussDbType.TimestampTzRange => "tstzrange",
                OpenGaussDbType.DateRange        => "daterange",

                // Built-in multirange types
                OpenGaussDbType.IntegerMultirange     => "int4multirange",
                OpenGaussDbType.BigIntMultirange      => "int8multirange",
                OpenGaussDbType.NumericMultirange     => "nummultirange",
                OpenGaussDbType.TimestampMultirange   => "tsmultirange",
                OpenGaussDbType.TimestampTzMultirange => "tstzmultirange",
                OpenGaussDbType.DateMultirange        => "datemultirange",

                // Internal types
                OpenGaussDbType.Int2Vector   => "int2vector",
                OpenGaussDbType.Oidvector    => "oidvector",
                OpenGaussDbType.PgLsn        => "pg_lsn",
                OpenGaussDbType.Tid          => "tid",
                OpenGaussDbType.InternalChar => "char",

                // Special types
                OpenGaussDbType.Unknown => "unknown",

                _ => opengaussDbType.HasFlag(OpenGaussDbType.Array)
                    ? OpenGaussDbTypeToDataTypeName(opengaussDbType & ~OpenGaussDbType.Array) + "[]"
                    : null // e.g. ranges
            };

        internal static OpenGaussDbType? DbTypeToOpenGaussDbType(DbType dbType)
            => dbType switch
            {
                DbType.AnsiString            => OpenGaussDbType.Text,
                DbType.Binary                => OpenGaussDbType.Bytea,
                DbType.Byte                  => OpenGaussDbType.Smallint,
                DbType.Boolean               => OpenGaussDbType.Boolean,
                DbType.Currency              => OpenGaussDbType.Money,
                DbType.Date                  => OpenGaussDbType.Date,
                DbType.DateTime              => LegacyTimestampBehavior ? OpenGaussDbType.Timestamp : OpenGaussDbType.TimestampTz,
                DbType.Decimal               => OpenGaussDbType.Numeric,
                DbType.VarNumeric            => OpenGaussDbType.Numeric,
                DbType.Double                => OpenGaussDbType.Double,
                DbType.Guid                  => OpenGaussDbType.Uuid,
                DbType.Int16                 => OpenGaussDbType.Smallint,
                DbType.Int32                 => OpenGaussDbType.Integer,
                DbType.Int64                 => OpenGaussDbType.Bigint,
                DbType.Single                => OpenGaussDbType.Real,
                DbType.String                => OpenGaussDbType.Text,
                DbType.Time                  => OpenGaussDbType.Time,
                DbType.AnsiStringFixedLength => OpenGaussDbType.Text,
                DbType.StringFixedLength     => OpenGaussDbType.Text,
                DbType.Xml                   => OpenGaussDbType.Xml,
                DbType.DateTime2             => OpenGaussDbType.Timestamp,
                DbType.DateTimeOffset        => OpenGaussDbType.TimestampTz,

                DbType.Object                => null,
                DbType.SByte                 => null,
                DbType.UInt16                => null,
                DbType.UInt32                => null,
                DbType.UInt64                => null,

                _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null)
            };

        internal static DbType OpenGaussDbTypeToDbType(OpenGaussDbType opengaussDbType)
            => opengaussDbType switch
            {
                // Numeric types
                OpenGaussDbType.Smallint    => DbType.Int16,
                OpenGaussDbType.Integer     => DbType.Int32,
                OpenGaussDbType.Bigint      => DbType.Int64,
                OpenGaussDbType.Real        => DbType.Single,
                OpenGaussDbType.Double      => DbType.Double,
                OpenGaussDbType.Numeric     => DbType.Decimal,
                OpenGaussDbType.Money       => DbType.Currency,

                // Text types
                OpenGaussDbType.Text        => DbType.String,
                OpenGaussDbType.Xml         => DbType.Xml,
                OpenGaussDbType.Varchar     => DbType.String,
                OpenGaussDbType.Char        => DbType.String,
                OpenGaussDbType.Name        => DbType.String,
                OpenGaussDbType.Refcursor   => DbType.String,
                OpenGaussDbType.Citext      => DbType.String,
                OpenGaussDbType.Jsonb       => DbType.Object,
                OpenGaussDbType.Json        => DbType.Object,
                OpenGaussDbType.JsonPath    => DbType.String,

                // Date/time types
                OpenGaussDbType.Timestamp   => LegacyTimestampBehavior ? DbType.DateTime : DbType.DateTime2,
                OpenGaussDbType.TimestampTz => LegacyTimestampBehavior ? DbType.DateTimeOffset : DbType.DateTime,
                OpenGaussDbType.Date        => DbType.Date,
                OpenGaussDbType.Time        => DbType.Time,

                // Misc data types
                OpenGaussDbType.Bytea       => DbType.Binary,
                OpenGaussDbType.Boolean     => DbType.Boolean,
                OpenGaussDbType.Uuid        => DbType.Guid,

                OpenGaussDbType.Unknown     => DbType.Object,

                _ => DbType.Object
            };

        #endregion Static translation tables
    }
}
