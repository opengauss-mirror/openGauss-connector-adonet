using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text.Json;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandlers;
using OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers;
using OpenGauss.NET.Internal.TypeHandlers.FullTextSearchHandlers;
using OpenGauss.NET.Internal.TypeHandlers.GeometricHandlers;
using OpenGauss.NET.Internal.TypeHandlers.InternalTypeHandlers;
using OpenGauss.NET.Internal.TypeHandlers.LTreeHandlers;
using OpenGauss.NET.Internal.TypeHandlers.NetworkHandlers;
using OpenGauss.NET.Internal.TypeHandlers.NumericHandlers;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;
using static OpenGauss.NET.Util.Statics;

namespace OpenGauss.NET.TypeMapping
{
    class BuiltInTypeHandlerResolver : TypeHandlerResolver
    {
        readonly OpenGaussConnector _connector;
        readonly OpenGaussDatabaseInfo _databaseInfo;

        static readonly Type ReadOnlyIPAddressType = IPAddress.Loopback.GetType();

        static readonly Dictionary<string, TypeMappingInfo> Mappings = new()
        {
            // Numeric types
            { "smallint",         new(OpenGaussDbType.Smallint, "smallint",         typeof(short), typeof(byte), typeof(sbyte)) },
            { "integer",          new(OpenGaussDbType.Integer,  "integer",          typeof(int)) },
            { "int",              new(OpenGaussDbType.Integer,  "integer",          typeof(int)) },
            { "bigint",           new(OpenGaussDbType.Bigint,   "bigint",           typeof(long)) },
            { "real",             new(OpenGaussDbType.Real,     "real",             typeof(float)) },
            { "double precision", new(OpenGaussDbType.Double,   "double precision", typeof(double)) },
            { "numeric",          new(OpenGaussDbType.Numeric,  "decimal",          typeof(decimal), typeof(BigInteger)) },
            { "decimal",          new(OpenGaussDbType.Numeric,  "decimal",          typeof(decimal), typeof(BigInteger)) },
            { "money",            new(OpenGaussDbType.Money,    "money") },

            // Text types
            { "text",              new(OpenGaussDbType.Text,      "text", typeof(string), typeof(char[]), typeof(char), typeof(ArraySegment<char>)) },
            { "xml",               new(OpenGaussDbType.Xml,       "xml") },
            { "character varying", new(OpenGaussDbType.Varchar,   "character varying") },
            { "varchar",           new(OpenGaussDbType.Varchar,   "character varying") },
            { "character",         new(OpenGaussDbType.Char,      "character") },
            { "name",              new(OpenGaussDbType.Name,      "name") },
            { "refcursor",         new(OpenGaussDbType.Refcursor, "refcursor") },
            { "citext",            new(OpenGaussDbType.Citext,    "citext") },
            { "jsonb",             new(OpenGaussDbType.Jsonb,     "jsonb", typeof(JsonDocument)) },
            { "json",              new(OpenGaussDbType.Json,      "json") },
            { "jsonpath",          new(OpenGaussDbType.JsonPath,  "jsonpath") },

            // Date/time types
#pragma warning disable 618 // OpenGaussDateTime is obsolete, remove in 7.0
            { "timestamp without time zone", new(OpenGaussDbType.Timestamp,   "timestamp without time zone", typeof(DateTime), typeof(OpenGaussDateTime)) },
            { "timestamp",                   new(OpenGaussDbType.Timestamp,   "timestamp without time zone", typeof(DateTime), typeof(OpenGaussDateTime)) },
#pragma warning disable 618
            { "timestamp with time zone",    new(OpenGaussDbType.TimestampTz, "timestamp with time zone",    typeof(DateTimeOffset)) },
            { "timestamptz",                 new(OpenGaussDbType.TimestampTz, "timestamp with time zone",    typeof(DateTimeOffset)) },
            { "date",                        new(OpenGaussDbType.Date,        "date",                        typeof(OpenGaussDate)
#if NET6_0_OR_GREATER
                , typeof(DateOnly)
#endif
            ) },
            { "time without time zone",      new(OpenGaussDbType.Time,        "timeout time zone"
#if NET6_0_OR_GREATER
                , typeof(TimeOnly)
#endif
            ) },
            { "time",                        new(OpenGaussDbType.Time,        "timeout time zone"
#if NET6_0_OR_GREATER
                , typeof(TimeOnly)
#endif
            ) },
            { "time with time zone",         new(OpenGaussDbType.TimeTz,      "time with time zone") },
            { "timetz",                      new(OpenGaussDbType.TimeTz,      "time with time zone") },
            { "interval",                    new(OpenGaussDbType.Interval,    "interval", typeof(TimeSpan), typeof(OpenGaussTimeSpan)) },

            { "timestamp without time zone[]", new(OpenGaussDbType.Array | OpenGaussDbType.Timestamp,   "timestamp without time zone[]") },
            { "timestamp with time zone[]",    new(OpenGaussDbType.Array | OpenGaussDbType.TimestampTz, "timestamp with time zone[]") },
            { "tsrange",                       new(OpenGaussDbType.Range | OpenGaussDbType.Timestamp,   "tsrange") },
            { "tstzrange",                     new(OpenGaussDbType.Range | OpenGaussDbType.TimestampTz, "tstzrange") },
            { "tsmultirange",                  new(OpenGaussDbType.Multirange | OpenGaussDbType.Timestamp,   "tsmultirange") },
            { "tstzmultirange",                new(OpenGaussDbType.Multirange | OpenGaussDbType.TimestampTz, "tstzmultirange") },

            // Network types
            { "cidr",      new(OpenGaussDbType.Cidr,     "cidr") },
#pragma warning disable 618
            { "inet",      new(OpenGaussDbType.Inet,     "inet", typeof(IPAddress), typeof((IPAddress Address, int Subnet)), typeof(OpenGaussInet), ReadOnlyIPAddressType) },
#pragma warning restore 618
            { "macaddr",   new(OpenGaussDbType.MacAddr,  "macaddr", typeof(PhysicalAddress)) },
            { "macaddr8",  new(OpenGaussDbType.MacAddr8, "macaddr8") },

            // Full-text search types
            { "tsquery",   new(OpenGaussDbType.TsQuery,  "tsquery",
                typeof(OpenGaussTsQuery), typeof(OpenGaussTsQueryAnd), typeof(OpenGaussTsQueryEmpty), typeof(OpenGaussTsQueryFollowedBy),
                typeof(OpenGaussTsQueryLexeme), typeof(OpenGaussTsQueryNot), typeof(OpenGaussTsQueryOr), typeof(OpenGaussTsQueryBinOp)
                ) },
            { "tsvector",  new(OpenGaussDbType.TsVector, "tsvector", typeof(OpenGaussTsVector)) },

            // Geometry types
            { "box",      new(OpenGaussDbType.Box,     "box",     typeof(OpenGaussBox)) },
            { "circle",   new(OpenGaussDbType.Circle,  "circle",  typeof(OpenGaussCircle)) },
            { "line",     new(OpenGaussDbType.Line,    "line",    typeof(OpenGaussLine)) },
            { "lseg",     new(OpenGaussDbType.LSeg,    "lseg",    typeof(OpenGaussLSeg)) },
            { "path",     new(OpenGaussDbType.Path,    "path",    typeof(OpenGaussPath)) },
            { "point",    new(OpenGaussDbType.Point,   "point",   typeof(OpenGaussPoint)) },
            { "polygon",  new(OpenGaussDbType.Polygon, "polygon", typeof(OpenGaussPolygon)) },

            // LTree types
            { "lquery",     new(OpenGaussDbType.LQuery,    "lquery") },
            { "ltree",      new(OpenGaussDbType.LTree,     "ltree") },
            { "ltxtquery",  new(OpenGaussDbType.LTxtQuery, "ltxtquery") },

            // UInt types
            { "oid",        new(OpenGaussDbType.Oid,       "oid") },
            { "xid",        new(OpenGaussDbType.Xid,       "xid") },
            { "xid8",       new(OpenGaussDbType.Xid8,      "xid8") },
            { "cid",        new(OpenGaussDbType.Cid,       "cid") },
            { "regtype",    new(OpenGaussDbType.Regtype,   "regtype") },
            { "regconfig",  new(OpenGaussDbType.Regconfig, "regconfig") },

            // Misc types
            { "boolean",     new(OpenGaussDbType.Boolean, "boolean", typeof(bool)) },
            { "bool",        new(OpenGaussDbType.Boolean, "boolean", typeof(bool)) },
            { "bytea",       new(OpenGaussDbType.Bytea,   "bytea", typeof(byte[]), typeof(ArraySegment<byte>)
#if !NETSTANDARD2_0
                , typeof(ReadOnlyMemory<byte>), typeof(Memory<byte>)
#endif
            ) },
            { "uuid",        new(OpenGaussDbType.Uuid,    "uuid", typeof(Guid)) },
            { "bit varying", new(OpenGaussDbType.Varbit,  "bit varying", typeof(BitArray), typeof(BitVector32)) },
            { "varbit",      new(OpenGaussDbType.Varbit,  "bit varying", typeof(BitArray), typeof(BitVector32)) },
            { "bit",         new(OpenGaussDbType.Bit,     "bit") },
            { "hstore",      new(OpenGaussDbType.Hstore,  "hstore", typeof(Dictionary<string, string?>), typeof(IDictionary<string, string?>)
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                , typeof(ImmutableDictionary<string, string?>)
#endif
            ) },

            // Internal types
            { "int2vector",  new(OpenGaussDbType.Int2Vector,   "int2vector") },
            { "oidvector",   new(OpenGaussDbType.Oidvector,    "oidvector") },
            { "pg_lsn",      new(OpenGaussDbType.PgLsn,        "pg_lsn", typeof(OpenGaussLogSequenceNumber)) },
            { "tid",         new(OpenGaussDbType.Tid,          "tid", typeof(OpenGaussTid)) },
            { "char",        new(OpenGaussDbType.InternalChar, "char") },

            // Special types
            { "unknown",  new(OpenGaussDbType.Unknown, "unknown") },
        };

        internal static void ResetMappings()
        {
            foreach (var mapping in Mappings)
                mapping.Value.Reset();
        }

        #region Cached handlers

        // Numeric types
        readonly Int16Handler _int16Handler;
        readonly Int32Handler _int32Handler;
        readonly Int64Handler _int64Handler;
        SingleHandler? _singleHandler;
        readonly DoubleHandler _doubleHandler;
        readonly NumericHandler _numericHandler;
        MoneyHandler? _moneyHandler;

        // Text types
        readonly TextHandler _textHandler;
        TextHandler? _xmlHandler;
        TextHandler? _varcharHandler;
        TextHandler? _charHandler;
        TextHandler? _nameHandler;
        TextHandler? _refcursorHandler;
        TextHandler? _citextHandler;
        JsonHandler? _jsonbHandler; // Note that old version of PG (and Redshift) don't have jsonb
        JsonHandler? _jsonHandler;
        JsonPathHandler? _jsonPathHandler;

        // Date/time types
        readonly TimestampHandler _timestampHandler;
        readonly TimestampTzHandler _timestampTzHandler;
        readonly DateHandler _dateHandler;
        TimeHandler? _timeHandler;
        TimeTzHandler? _timeTzHandler;
        IntervalHandler? _intervalHandler;

        // Network types
        CidrHandler? _cidrHandler;
        InetHandler? _inetHandler;
        MacaddrHandler? _macaddrHandler;
        MacaddrHandler? _macaddr8Handler;

        // Full-text search types
        TsQueryHandler? _tsQueryHandler;
        TsVectorHandler? _tsVectorHandler;

        // Geometry types
        BoxHandler? _boxHandler;
        CircleHandler? _circleHandler;
        LineHandler? _lineHandler;
        LineSegmentHandler? _lineSegmentHandler;
        PathHandler? _pathHandler;
        PointHandler? _pointHandler;
        PolygonHandler? _polygonHandler;

        // LTree types
        LQueryHandler? _lQueryHandler;
        LTreeHandler? _lTreeHandler;
        LTxtQueryHandler? _lTxtQueryHandler;

        // UInt types
        UInt32Handler? _oidHandler;
        UInt32Handler? _xidHandler;
        UInt64Handler? _xid8Handler;
        UInt32Handler? _cidHandler;
        UInt32Handler? _regtypeHandler;
        UInt32Handler? _regconfigHandler;

        // Misc types
        readonly BoolHandler _boolHandler;
        ByteaHandler? _byteaHandler;
        UuidHandler? _uuidHandler;
        BitStringHandler? _bitVaryingHandler;
        BitStringHandler? _bitHandler;
        RecordHandler? _recordHandler;
        VoidHandler? _voidHandler;
        HstoreHandler? _hstoreHandler;

        // Internal types
        Int2VectorHandler? _int2VectorHandler;
        OIDVectorHandler? _oidVectorHandler;
        PgLsnHandler? _pgLsnHandler;
        TidHandler? _tidHandler;
        InternalCharHandler? _internalCharHandler;

        // Special types
        UnknownTypeHandler? _unknownHandler;

        // Complex type handlers over timestamp/timestamptz (because DateTime is value-dependent)
        OpenGaussTypeHandler? _timestampArrayHandler;
        OpenGaussTypeHandler? _timestampTzArrayHandler;
        OpenGaussTypeHandler? _timestampRangeHandler;
        OpenGaussTypeHandler? _timestampTzRangeHandler;
        OpenGaussTypeHandler? _timestampMultirangeHandler;
        OpenGaussTypeHandler? _timestampTzMultirangeHandler;

        #endregion Cached handlers

        internal BuiltInTypeHandlerResolver(OpenGaussConnector connector)
        {
            _connector = connector;
            _databaseInfo = connector.DatabaseInfo;

            // Eagerly instantiate some handlers for very common types so we don't need to check later
            _int16Handler = new Int16Handler(PgType("smallint"));
            _int32Handler = new Int32Handler(PgType("integer"));
            _int64Handler = new Int64Handler(PgType("bigint"));
            _doubleHandler = new DoubleHandler(PgType("double precision"));
            _numericHandler = new NumericHandler(PgType("numeric"));
            _textHandler ??= new TextHandler(PgType("text"), _connector.TextEncoding);
            _timestampHandler ??= new TimestampHandler(PgType("timestamp without time zone"));
            _timestampTzHandler ??= new TimestampTzHandler(PgType("timestamp with time zone"));
            _dateHandler ??= new DateHandler(PgType("date"));
            _boolHandler ??= new BoolHandler(PgType("boolean"));
        }

        public override OpenGaussTypeHandler? ResolveByDataTypeName(string typeName)
            => typeName switch
            {
                // Numeric types
                "smallint"             => _int16Handler,
                "integer" or "int"     => _int32Handler,
                "bigint"               => _int64Handler,
                "real"                 => SingleHandler(),
                "double precision"     => _doubleHandler,
                "numeric" or "decimal" => _numericHandler,
                "money"                => MoneyHandler(),

                // Text types
                "text"                           => _textHandler,
                "xml"                            => XmlHandler(),
                "varchar" or "character varying" => VarcharHandler(),
                "character"                      => CharHandler(),
                "name"                           => NameHandler(),
                "refcursor"                      => RefcursorHandler(),
                "citext"                         => CitextHandler(),
                "jsonb"                          => JsonbHandler(),
                "json"                           => JsonHandler(),
                "jsonpath"                       => JsonPathHandler(),

                // Date/time types
                "timestamp" or "timestamp without time zone" => _timestampHandler,
                "timestamptz" or "timestamp with time zone"  => _timestampTzHandler,
                "date"                                       => _dateHandler,
                "time without time zone"                     => TimeHandler(),
                "time with time zone"                        => TimeTzHandler(),
                "interval"                                   => IntervalHandler(),

                // Network types
                "cidr"     => CidrHandler(),
                "inet"     => InetHandler(),
                "macaddr"  => MacaddrHandler(),
                "macaddr8" => Macaddr8Handler(),

                // Full-text search types
                "tsquery"  => TsQueryHandler(),
                "tsvector" => TsVectorHandler(),

                // Geometry types
                "box"     => BoxHandler(),
                "circle"  => CircleHandler(),
                "line"    => LineHandler(),
                "lseg"    => LineSegmentHandler(),
                "path"    => PathHandler(),
                "point"   => PointHandler(),
                "polygon" => PolygonHandler(),

                // LTree types
                "lquery"    => LQueryHandler(),
                "ltree"     => LTreeHandler(),
                "ltxtquery" => LTxtHandler(),

                // UInt types
                "oid"       => OidHandler(),
                "xid"       => XidHandler(),
                "xid8"      => Xid8Handler(),
                "cid"       => CidHandler(),
                "regtype"   => RegtypeHandler(),
                "regconfig" => RegconfigHandler(),

                // Misc types
                "bool" or "boolean"       => _boolHandler,
                "bytea"                   => ByteaHandler(),
                "uuid"                    => UuidHandler(),
                "bit varying" or "varbit" => BitVaryingHandler(),
                "bit"                     => BitHandler(),
                "hstore"                  => HstoreHandler(),

                // Internal types
                "int2vector" => Int2VectorHandler(),
                "oidvector"  => OidVectorHandler(),
                "pg_lsn"     => PgLsnHandler(),
                "tid"        => TidHandler(),
                "char"       => InternalCharHandler(),
                "record"     => RecordHandler(),
                "void"       => VoidHandler(),

                "unknown"    => UnknownHandler(),

                _ => null
            };

        public override OpenGaussTypeHandler? ResolveByClrType(Type type)
            => ClrTypeToDataTypeNameTable.TryGetValue(type, out var dataTypeName) && ResolveByDataTypeName(dataTypeName) is { } handler
                ? handler
                : null;

        static readonly Dictionary<Type, string> ClrTypeToDataTypeNameTable;

        static BuiltInTypeHandlerResolver()
        {
            ClrTypeToDataTypeNameTable = new()
            {
                // Numeric types
                { typeof(byte),       "smallint" },
                { typeof(short),      "smallint" },
                { typeof(int),        "integer" },
                { typeof(long),       "bigint" },
                { typeof(float),      "real" },
                { typeof(double),     "double precision" },
                { typeof(decimal),    "decimal" },
                { typeof(BigInteger), "decimal" },

                // Text types
                { typeof(string),             "text" },
                { typeof(char[]),             "text" },
                { typeof(char),               "text" },
                { typeof(ArraySegment<char>), "text" },
                { typeof(JsonDocument),       "jsonb" },

                // Date/time types
                // The DateTime entry is for LegacyTimestampBehavior mode only. In regular mode we resolve through
                // ResolveValueDependentValue below
                { typeof(DateTime),       "timestamp without time zone" },
                { typeof(DateTimeOffset), "timestamp with time zone" },
#if NET6_0_OR_GREATER
                { typeof(DateOnly),       "date" },
                { typeof(TimeOnly),       "time without time zone" },
#endif
                { typeof(TimeSpan),       "interval" },
                { typeof(OpenGaussInterval), "interval" },
#pragma warning disable 618 // OpenGaussDateTime and OpenGaussDate are obsolete, remove in 7.0
                { typeof(OpenGaussDateTime), "timestamp without time zone" },
                { typeof(OpenGaussDate),     "date" },
                { typeof(OpenGaussTimeSpan), "interval" },
#pragma warning restore 618

                // Network types
                { typeof(IPAddress),                       "inet" },
                // See ReadOnlyIPAddress below
                { typeof((IPAddress Address, int Subnet)), "inet" },
#pragma warning disable 618
                { typeof(OpenGaussInet),                      "inet" },
#pragma warning restore 618
                { typeof(PhysicalAddress),                 "macaddr" },

                // Full-text types
                { typeof(OpenGaussTsQuery),  "tsquery" },
                { typeof(OpenGaussTsVector), "tsvector" },

                // Geometry types
                { typeof(OpenGaussBox),     "box" },
                { typeof(OpenGaussCircle),  "circle" },
                { typeof(OpenGaussLine),    "line" },
                { typeof(OpenGaussLSeg),    "lseg" },
                { typeof(OpenGaussPath),    "path" },
                { typeof(OpenGaussPoint),   "point" },
                { typeof(OpenGaussPolygon), "polygon" },

                // Misc types
                { typeof(bool),                 "boolean" },
                { typeof(byte[]),               "bytea" },
                { typeof(ArraySegment<byte>),   "bytea" },
#if !NETSTANDARD2_0
                { typeof(ReadOnlyMemory<byte>), "bytea" },
                { typeof(Memory<byte>),         "bytea" },
#endif
                { typeof(Guid),                                "uuid" },
                { typeof(BitArray),                            "bit varying" },
                { typeof(BitVector32),                         "bit varying" },
                { typeof(Dictionary<string, string>),          "hstore" },
#if !NETSTANDARD2_0 && !NETSTANDARD2_1
                { typeof(ImmutableDictionary<string, string>), "hstore" },
#endif

                // Internal types
                { typeof(OpenGaussLogSequenceNumber), "pg_lsn" },
                { typeof(OpenGaussTid),               "tid" },
                { typeof(DBNull),                  "unknown" }
            };

            // Recent versions of .NET Core have an internal ReadOnlyIPAddress type (returned e.g. for IPAddress.Loopback)
            // But older versions don't have it
            if (ReadOnlyIPAddressType != typeof(IPAddress))
                ClrTypeToDataTypeNameTable[ReadOnlyIPAddressType] = "inet";

            if (LegacyTimestampBehavior)
                ClrTypeToDataTypeNameTable[typeof(DateTime)] = "timestamp without time zone";
        }

        public override OpenGaussTypeHandler? ResolveValueDependentValue(object value)
        {
            // In LegacyTimestampBehavior, DateTime isn't value-dependent, and handled above in ClrTypeToDataTypeNameTable like other types
            if (LegacyTimestampBehavior)
                return null;

            return value switch
            {
                DateTime dateTime => dateTime.Kind == DateTimeKind.Utc ? _timestampTzHandler : _timestampHandler,

                // For arrays/lists, return timestamp or timestamptz based on the kind of the first DateTime; if the user attempts to
                // mix incompatible Kinds, that will fail during validation. For empty arrays it doesn't matter.
                IList<DateTime> array => ArrayHandler(array.Count == 0 ? DateTimeKind.Unspecified : array[0].Kind),

                OpenGaussRange<DateTime> range => RangeHandler(!range.LowerBoundInfinite ? range.LowerBound.Kind :
                    !range.UpperBoundInfinite ? range.UpperBound.Kind : DateTimeKind.Unspecified),

                OpenGaussRange<DateTime>[] multirange => MultirangeHandler(GetMultirangeKind(multirange)),
                _ => null
            };

            OpenGaussTypeHandler ArrayHandler(DateTimeKind kind)
                => kind == DateTimeKind.Utc
                    ? _timestampTzArrayHandler ??= _timestampTzHandler.CreateArrayHandler(
                        (PostgresArrayType)PgType("timestamp with time zone[]"), _connector.Settings.ArrayNullabilityMode)
                    : _timestampArrayHandler ??= _timestampHandler.CreateArrayHandler(
                        (PostgresArrayType)PgType("timestamp without time zone[]"), _connector.Settings.ArrayNullabilityMode);

            OpenGaussTypeHandler RangeHandler(DateTimeKind kind)
                => kind == DateTimeKind.Utc
                    ? _timestampTzRangeHandler ??= _timestampTzHandler.CreateRangeHandler((PostgresRangeType)PgType("tstzrange"))
                    : _timestampRangeHandler ??= _timestampHandler.CreateRangeHandler((PostgresRangeType)PgType("tsrange"));

            OpenGaussTypeHandler MultirangeHandler(DateTimeKind kind)
                => kind == DateTimeKind.Utc
                    ? _timestampTzMultirangeHandler ??= _timestampTzHandler.CreateMultirangeHandler((PostgresMultirangeType)PgType("tstzmultirange"))
                    : _timestampMultirangeHandler ??= _timestampHandler.CreateMultirangeHandler((PostgresMultirangeType)PgType("tsmultirange"));
        }

        static DateTimeKind GetRangeKind(OpenGaussRange<DateTime> range)
            => !range.LowerBoundInfinite
                ? range.LowerBound.Kind
                : !range.UpperBoundInfinite
                    ? range.UpperBound.Kind
                    : DateTimeKind.Unspecified;

        static DateTimeKind GetMultirangeKind(OpenGaussRange<DateTime>[] multirange)
        {
            for (var i = 0; i < multirange.Length; i++)
                if (!multirange[i].IsEmpty)
                    return GetRangeKind(multirange[i]);

            return DateTimeKind.Unspecified;
        }

        internal static string? ValueDependentValueToDataTypeName(object value)
        {
            // In LegacyTimestampBehavior, DateTime isn't value-dependent, and handled above in ClrTypeToDataTypeNameTable like other types
            if (LegacyTimestampBehavior)
                return null;

            return value switch
            {
                DateTime dateTime => dateTime.Kind == DateTimeKind.Utc ? "timestamp with time zone" : "timestamp without time zone",

                // For arrays/lists, return timestamp or timestamptz based on the kind of the first DateTime; if the user attempts to
                // mix incompatible Kinds, that will fail during validation. For empty arrays it doesn't matter.
                IList<DateTime> array => array.Count == 0
                    ? "timestamp without time zone[]"
                    : array[0].Kind == DateTimeKind.Utc ? "timestamp with time zone[]" : "timestamp without time zone[]",

                OpenGaussRange<DateTime> range => GetRangeKind(range) == DateTimeKind.Utc ? "tstzrange" : "tsrange",

                OpenGaussRange<DateTime>[] multirange => GetMultirangeKind(multirange) == DateTimeKind.Utc ? "tstzmultirange" : "tsmultirange",

                _ => null
            };
        }

        public override OpenGaussTypeHandler? ResolveValueTypeGenerically<T>(T value)
        {
            // This method only ever gets called for value types, and relies on the JIT specializing the method for T by eliding all the
            // type checks below.

            // Numeric types
            if (typeof(T) == typeof(byte))
                return _int16Handler;
            if (typeof(T) == typeof(short))
                return _int16Handler;
            if (typeof(T) == typeof(int))
                return _int32Handler;
            if (typeof(T) == typeof(long))
                return _int64Handler;
            if (typeof(T) == typeof(float))
                return SingleHandler();
            if (typeof(T) == typeof(double))
                return _doubleHandler;
            if (typeof(T) == typeof(decimal))
                return _numericHandler;
            if (typeof(T) == typeof(BigInteger))
                return _numericHandler;

            // Text types
            if (typeof(T) == typeof(char))
                return _textHandler;
            if (typeof(T) == typeof(ArraySegment<char>))
                return _textHandler;
            if (typeof(T) == typeof(JsonDocument))
                return JsonbHandler();

            // Date/time types
            // No resolution for DateTime, since that's value-dependent (Kind)
            if (typeof(T) == typeof(DateTimeOffset))
                return _timestampTzHandler;
#if NET6_0_OR_GREATER
            if (typeof(T) == typeof(DateOnly))
                return _dateHandler;
            if (typeof(T) == typeof(TimeOnly))
                return _timeHandler;
#endif
            if (typeof(T) == typeof(TimeSpan))
                return _intervalHandler;
            if (typeof(T) == typeof(OpenGaussInterval))
                return _intervalHandler;
#pragma warning disable 618 // OpenGaussDate and OpenGaussTimeSpan are obsolete, remove in 7.0
            if (typeof(T) == typeof(OpenGaussDate))
                return _dateHandler;
            if (typeof(T) == typeof(OpenGaussTimeSpan))
                return _intervalHandler;
#pragma warning restore 618

            // Network types
            if (typeof(T) == typeof(IPAddress))
                return InetHandler();
            if (typeof(T) == typeof(PhysicalAddress))
                return _macaddrHandler;
            if (typeof(T) == typeof(TimeSpan))
                return _intervalHandler;

            // Geometry types
            if (typeof(T) == typeof(OpenGaussBox))
                return BoxHandler();
            if (typeof(T) == typeof(OpenGaussCircle))
                return CircleHandler();
            if (typeof(T) == typeof(OpenGaussLine))
                return LineHandler();
            if (typeof(T) == typeof(OpenGaussLSeg))
                return LineSegmentHandler();
            if (typeof(T) == typeof(OpenGaussPath))
                return PathHandler();
            if (typeof(T) == typeof(OpenGaussPoint))
                return PointHandler();
            if (typeof(T) == typeof(OpenGaussPolygon))
                return PolygonHandler();

            // Misc types
            if (typeof(T) == typeof(bool))
                return _boolHandler;
            if (typeof(T) == typeof(Guid))
                return UuidHandler();
            if (typeof(T) == typeof(BitVector32))
                return BitVaryingHandler();

            // Internal types
            if (typeof(T) == typeof(OpenGaussLogSequenceNumber))
                return PgLsnHandler();
            if (typeof(T) == typeof(OpenGaussTid))
                return TidHandler();
            if (typeof(T) == typeof(DBNull))
                return UnknownHandler();

            return null;
        }

        internal static string? ClrTypeToDataTypeName(Type type)
            => ClrTypeToDataTypeNameTable.TryGetValue(type, out var dataTypeName) ? dataTypeName : null;

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => DoGetMappingByDataTypeName(dataTypeName);

        internal static TypeMappingInfo? DoGetMappingByDataTypeName(string dataTypeName)
            => Mappings.TryGetValue(dataTypeName, out var mapping) ? mapping : null;

        PostgresType PgType(string pgTypeName) => _databaseInfo.GetPostgresTypeByName(pgTypeName);

        #region Handler accessors

        // Numeric types
        OpenGaussTypeHandler SingleHandler() => _singleHandler ??= new SingleHandler(PgType("real"));
        OpenGaussTypeHandler MoneyHandler()  => _moneyHandler ??= new MoneyHandler(PgType("money"));

        // Text types
        OpenGaussTypeHandler XmlHandler()       => _xmlHandler ??= new TextHandler(PgType("xml"), _connector.TextEncoding);
        OpenGaussTypeHandler VarcharHandler()   => _varcharHandler ??= new TextHandler(PgType("character varying"), _connector.TextEncoding);
        OpenGaussTypeHandler CharHandler()      => _charHandler ??= new TextHandler(PgType("character"), _connector.TextEncoding);
        OpenGaussTypeHandler NameHandler()      => _nameHandler ??= new TextHandler(PgType("name"), _connector.TextEncoding);
        OpenGaussTypeHandler RefcursorHandler() => _refcursorHandler ??= new TextHandler(PgType("refcursor"), _connector.TextEncoding);
        OpenGaussTypeHandler? CitextHandler()   => _citextHandler ??= _databaseInfo.TryGetPostgresTypeByName("citext", out var pgType)
            ? new TextHandler(pgType, _connector.TextEncoding)
            : null;
        OpenGaussTypeHandler JsonbHandler()     => _jsonbHandler ??= new JsonHandler(PgType("jsonb"), _connector.TextEncoding, isJsonb: true);
        OpenGaussTypeHandler JsonHandler()      => _jsonHandler ??= new JsonHandler(PgType("json"), _connector.TextEncoding, isJsonb: false);
        OpenGaussTypeHandler JsonPathHandler()  => _jsonPathHandler ??= new JsonPathHandler(PgType("jsonpath"), _connector.TextEncoding);

        // Date/time types
        OpenGaussTypeHandler TimeHandler()     => _timeHandler ??= new TimeHandler(PgType("time without time zone"));
        OpenGaussTypeHandler TimeTzHandler()   => _timeTzHandler ??= new TimeTzHandler(PgType("time with time zone"));
        OpenGaussTypeHandler IntervalHandler() => _intervalHandler ??= new IntervalHandler(PgType("interval"));

        // Network types
        OpenGaussTypeHandler CidrHandler()     => _cidrHandler ??= new CidrHandler(PgType("cidr"));
        OpenGaussTypeHandler InetHandler()     => _inetHandler ??= new InetHandler(PgType("inet"));
        OpenGaussTypeHandler MacaddrHandler()  => _macaddrHandler ??= new MacaddrHandler(PgType("macaddr"));
        OpenGaussTypeHandler Macaddr8Handler() => _macaddr8Handler ??= new MacaddrHandler(PgType("macaddr8"));

        // Full-text search types
        OpenGaussTypeHandler TsQueryHandler()  => _tsQueryHandler ??= new TsQueryHandler(PgType("tsquery"));
        OpenGaussTypeHandler TsVectorHandler() => _tsVectorHandler ??= new TsVectorHandler(PgType("tsvector"));

        // Geometry types
        OpenGaussTypeHandler BoxHandler()         => _boxHandler ??= new BoxHandler(PgType("box"));
        OpenGaussTypeHandler CircleHandler()      => _circleHandler ??= new CircleHandler(PgType("circle"));
        OpenGaussTypeHandler LineHandler()        => _lineHandler ??= new LineHandler(PgType("line"));
        OpenGaussTypeHandler LineSegmentHandler() => _lineSegmentHandler ??= new LineSegmentHandler(PgType("lseg"));
        OpenGaussTypeHandler PathHandler()        => _pathHandler ??= new PathHandler(PgType("path"));
        OpenGaussTypeHandler PointHandler()       => _pointHandler ??= new PointHandler(PgType("point"));
        OpenGaussTypeHandler PolygonHandler()     => _polygonHandler ??= new PolygonHandler(PgType("polygon"));

        // LTree types
        OpenGaussTypeHandler? LQueryHandler() => _lQueryHandler ??= _databaseInfo.TryGetPostgresTypeByName("lquery", out var pgType)
            ? new LQueryHandler(pgType, _connector.TextEncoding)
            : null;
        OpenGaussTypeHandler? LTreeHandler()  => _lTreeHandler ??= _databaseInfo.TryGetPostgresTypeByName("ltree", out var pgType)
            ? new LTreeHandler(pgType, _connector.TextEncoding)
            : null;
        OpenGaussTypeHandler? LTxtHandler()   => _lTxtQueryHandler ??= _databaseInfo.TryGetPostgresTypeByName("ltxtquery", out var pgType)
            ? new LTxtQueryHandler(pgType, _connector.TextEncoding)
            : null;

        // UInt types
        OpenGaussTypeHandler OidHandler()       => _oidHandler ??= new UInt32Handler(PgType("oid"));
        OpenGaussTypeHandler XidHandler()       => _xidHandler ??= new UInt32Handler(PgType("xid"));
        OpenGaussTypeHandler Xid8Handler()      => _xid8Handler ??= new UInt64Handler(PgType("xid8"));
        OpenGaussTypeHandler CidHandler()       => _cidHandler ??= new UInt32Handler(PgType("cid"));
        OpenGaussTypeHandler RegtypeHandler()   => _regtypeHandler ??= new UInt32Handler(PgType("regtype"));
        OpenGaussTypeHandler RegconfigHandler() => _regconfigHandler ??= new UInt32Handler(PgType("regconfig"));

        // Misc types
        OpenGaussTypeHandler ByteaHandler()      => _byteaHandler ??= new ByteaHandler(PgType("bytea"));
        OpenGaussTypeHandler UuidHandler()       => _uuidHandler ??= new UuidHandler(PgType("uuid"));
        OpenGaussTypeHandler BitVaryingHandler() => _bitVaryingHandler ??= new BitStringHandler(PgType("bit varying"));
        OpenGaussTypeHandler BitHandler()        => _bitHandler ??= new BitStringHandler(PgType("bit"));
        OpenGaussTypeHandler? HstoreHandler()    => _hstoreHandler ??= _databaseInfo.TryGetPostgresTypeByName("hstore", out var pgType)
            ? new HstoreHandler(pgType, _textHandler)
            : null;

        // Internal types
        OpenGaussTypeHandler Int2VectorHandler()   => _int2VectorHandler ??= new Int2VectorHandler(PgType("int2vector"), PgType("smallint"));
        OpenGaussTypeHandler OidVectorHandler()    => _oidVectorHandler ??= new OIDVectorHandler(PgType("oidvector"), PgType("oid"));
        OpenGaussTypeHandler PgLsnHandler()        => _pgLsnHandler ??= new PgLsnHandler(PgType("pg_lsn"));
        OpenGaussTypeHandler TidHandler()          => _tidHandler ??= new TidHandler(PgType("tid"));
        OpenGaussTypeHandler InternalCharHandler() => _internalCharHandler ??= new InternalCharHandler(PgType("char"));
        OpenGaussTypeHandler RecordHandler()       => _recordHandler ??= new RecordHandler(PgType("record"), _connector.TypeMapper);
        OpenGaussTypeHandler VoidHandler()         => _voidHandler ??= new VoidHandler(PgType("void"));

        OpenGaussTypeHandler UnknownHandler() => _unknownHandler ??= new UnknownTypeHandler(_connector);

        #endregion Handler accessors
    }
}
