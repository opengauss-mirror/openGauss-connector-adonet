using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandlers;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.Internal.TypeMapping;
using OpenGauss.NET.Logging;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.TypeMapping
{
    sealed class ConnectorTypeMapper : TypeMapperBase
    {
        internal OpenGaussConnector Connector { get; }
        readonly object _writeLock = new();

        OpenGaussDatabaseInfo? _databaseInfo;

        internal OpenGaussDatabaseInfo DatabaseInfo
        {
            get => _databaseInfo ?? throw new InvalidOperationException("Internal error: this type mapper hasn't yet been bound to a database info object");
            set
            {
                _databaseInfo = value;
                Reset();
            }
        }

        volatile List<TypeHandlerResolver> _resolvers;
        internal OpenGaussTypeHandler UnrecognizedTypeHandler { get; }

        readonly ConcurrentDictionary<uint, OpenGaussTypeHandler> _handlersByOID = new();
        readonly ConcurrentDictionary<OpenGaussDbType, OpenGaussTypeHandler> _handlersByOpenGaussDbType = new();
        readonly ConcurrentDictionary<Type, OpenGaussTypeHandler> _handlersByClrType = new();
        readonly ConcurrentDictionary<string, OpenGaussTypeHandler> _handlersByDataTypeName = new();

        readonly Dictionary<uint, TypeMappingInfo> _userTypeMappings = new();

        /// <summary>
        /// Copy of <see cref="GlobalTypeMapper.ChangeCounter"/> at the time when this
        /// mapper was created, to detect mapping changes. If changes are made to this connection's
        /// mapper, the change counter is set to -1.
        /// </summary>
        internal int ChangeCounter { get; private set; }

        static readonly OpenGaussLogger Log = OpenGaussLogManager.CreateLogger(nameof(ConnectorTypeMapper));

        #region Construction

        internal ConnectorTypeMapper(OpenGaussConnector connector) : base(GlobalTypeMapper.Instance.DefaultNameTranslator)
        {
            Connector = connector;
            UnrecognizedTypeHandler = new UnknownTypeHandler(Connector);
            _resolvers = new List<TypeHandlerResolver>();
        }

        #endregion Constructors

        #region Type handler lookup

        /// <summary>
        /// Looks up a type handler by its PostgreSQL type's OID.
        /// </summary>
        /// <param name="oid">A PostgreSQL type OID</param>
        /// <returns>A type handler that can be used to encode and decode values.</returns>
        internal OpenGaussTypeHandler ResolveByOID(uint oid)
            => TryResolveByOID(oid, out var result) ? result : UnrecognizedTypeHandler;

        internal bool TryResolveByOID(uint oid, [NotNullWhen(true)] out OpenGaussTypeHandler? handler)
        {
            if (_handlersByOID.TryGetValue(oid, out handler))
                return true;

            if (!DatabaseInfo.ByOID.TryGetValue(oid, out var pgType))
                return false;

            lock (_writeLock)
            {
                if ((handler = ResolveByDataTypeName(pgType.Name, throwOnError: false)) is not null)
                {
                    _handlersByOID[oid] = handler;
                    return true;
                }

                handler = null;
                return false;
            }
        }

        internal OpenGaussTypeHandler ResolveByOpenGaussDbType(OpenGaussDbType opengaussDbType)
        {
            if (_handlersByOpenGaussDbType.TryGetValue(opengaussDbType, out var handler))
                return handler;

            lock (_writeLock)
            {
                if (TryResolve(opengaussDbType, out handler))
                    return _handlersByOpenGaussDbType[opengaussDbType] = handler;

                if (opengaussDbType.HasFlag(OpenGaussDbType.Array))
                {
                    if (!TryResolve(opengaussDbType & ~OpenGaussDbType.Array, out var elementHandler))
                        throw new ArgumentException($"Array type over OpenGaussDbType {opengaussDbType} isn't supported by OpenGauss");

                    if (elementHandler.PostgresType.Array is not { } pgArrayType)
                        throw new ArgumentException(
                            $"No array type could be found in the database for element {elementHandler.PostgresType}");

                    return _handlersByOpenGaussDbType[opengaussDbType] =
                        elementHandler.CreateArrayHandler(pgArrayType, Connector.Settings.ArrayNullabilityMode);
                }

                if (opengaussDbType.HasFlag(OpenGaussDbType.Range))
                {
                    if (!TryResolve(opengaussDbType & ~OpenGaussDbType.Range, out var subtypeHandler))
                        throw new ArgumentException($"Range type over OpenGaussDbType {opengaussDbType} isn't supported by OpenGauss");

                    if (subtypeHandler.PostgresType.Range is not { } pgRangeType)
                        throw new ArgumentException(
                            $"No range type could be found in the database for subtype {subtypeHandler.PostgresType}");

                    return _handlersByOpenGaussDbType[opengaussDbType] = subtypeHandler.CreateRangeHandler(pgRangeType);
                }

                if (opengaussDbType.HasFlag(OpenGaussDbType.Multirange))
                {
                    if (!TryResolve(opengaussDbType & ~OpenGaussDbType.Multirange, out var subtypeHandler))
                        throw new ArgumentException($"Multirange type over OpenGaussDbType {opengaussDbType} isn't supported by OpenGauss");

                    if (subtypeHandler.PostgresType.Range?.Multirange is not { } pgMultirangeType)
                        throw new ArgumentException(
                            $"No multirange type could be found in the database for subtype {subtypeHandler.PostgresType}");

                    return _handlersByOpenGaussDbType[opengaussDbType] = subtypeHandler.CreateMultirangeHandler(pgMultirangeType);
                }

                throw new OpenGaussException($"The OpenGaussDbType '{opengaussDbType}' isn't present in your database. " +
                                          "You may need to install an extension or upgrade to a newer version.");

                bool TryResolve(OpenGaussDbType opengaussDbType, [NotNullWhen(true)] out OpenGaussTypeHandler? handler)
                {
                    if (GlobalTypeMapper.OpenGaussDbTypeToDataTypeName(opengaussDbType) is { } dataTypeName)
                    {
                        foreach (var resolver in _resolvers)
                        {
                            try
                            {
                                if ((handler = resolver.ResolveByDataTypeName(dataTypeName)) is not null)
                                    return true;
                            }
                            catch (Exception e)
                            {
                                Log.Error($"Type resolver {resolver.GetType().Name} threw exception while resolving OpenGaussDbType {opengaussDbType}", e);
                            }
                        }
                    }

                    handler = null;
                    return false;
                }
            }
        }

        internal OpenGaussTypeHandler ResolveByDataTypeName(string typeName)
            => ResolveByDataTypeName(typeName, throwOnError: true)!;

        OpenGaussTypeHandler? ResolveByDataTypeName(string typeName, bool throwOnError)
        {
            if (_handlersByDataTypeName.TryGetValue(typeName, out var handler))
                return handler;

            lock (_writeLock)
            {
                foreach (var resolver in _resolvers)
                {
                    try
                    {
                        if ((handler = resolver.ResolveByDataTypeName(typeName)) is not null)
                            return _handlersByDataTypeName[typeName] = handler;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Type resolver {resolver.GetType().Name} threw exception while resolving data type name {typeName}", e);
                    }
                }

                if (DatabaseInfo.GetPostgresTypeByName(typeName) is not { } pgType)
                    throw new NotSupportedException("Could not find PostgreSQL type " + typeName);

                switch (pgType)
                {
                case PostgresArrayType pgArrayType:
                {
                    var elementHandler = ResolveByOID(pgArrayType.Element.OID);
                    return _handlersByDataTypeName[typeName] =
                        elementHandler.CreateArrayHandler(pgArrayType, Connector.Settings.ArrayNullabilityMode);
                }

                case PostgresRangeType pgRangeType:
                {
                    var subtypeHandler = ResolveByOID(pgRangeType.Subtype.OID);
                    return _handlersByDataTypeName[typeName] = subtypeHandler.CreateRangeHandler(pgRangeType);
                }

                case PostgresMultirangeType pgMultirangeType:
                {
                    var subtypeHandler = ResolveByOID(pgMultirangeType.Subrange.Subtype.OID);
                    return _handlersByDataTypeName[typeName] = subtypeHandler.CreateMultirangeHandler(pgMultirangeType);
                }

                case PostgresEnumType pgEnumType:
                {
                    // A mapped enum would have been registered in _extraHandlersByDataTypeName and bound above - this is unmapped.
                    return _handlersByDataTypeName[typeName] =
                        new UnmappedEnumHandler(pgEnumType, DefaultNameTranslator, Connector.TextEncoding);
                }

                case PostgresDomainType pgDomainType:
                    return _handlersByDataTypeName[typeName] = ResolveByOID(pgDomainType.BaseType.OID);

                case PostgresBaseType pgBaseType:
                    return throwOnError
                        ? throw new NotSupportedException($"PostgreSQL type '{pgBaseType}' isn't supported by OpenGauss")
                        : null;

                case PostgresCompositeType pgCompositeType:
                    // We don't support writing unmapped composite types, but we do support reading unmapped composite types.
                    // So when we're invoked from ResolveOID (which is the read path), we don't want to raise an exception.
                    return throwOnError
                        ? throw new NotSupportedException(
                            $"Composite type '{pgCompositeType}' must be mapped with OpenGauss before being used, see the docs.")
                        : null;

                default:
                    throw new ArgumentOutOfRangeException($"Unhandled PostgreSQL type type: {pgType.GetType()}");
                }
            }
        }

        internal OpenGaussTypeHandler ResolveByValue<T>(T value)
        {
            if (value is null)
                return ResolveByClrType(typeof(T));

            if (typeof(T).IsValueType)
            {
                // Attempt to resolve value types generically via the resolver. This is the efficient fast-path, where we don't even need to
                // do a dictionary lookup (the JIT elides type checks in generic methods for value types)
                OpenGaussTypeHandler? handler;

                foreach (var resolver in _resolvers)
                {
                    try
                    {
                        if ((handler = resolver.ResolveValueTypeGenerically(value)) is not null)
                            return handler;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Type resolver {resolver.GetType().Name} threw exception while resolving value with type {typeof(T)}", e);
                    }
                }

                // There may still be some value types not resolved by the above, e.g. OpenGaussRange
            }

            // Value types would have been resolved above, so this is a reference type - no JIT optimizations.
            // We go through the regular logic (and there's no boxing).
            return ResolveByValue((object)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OpenGaussTypeHandler ResolveByValue(object value)
        {
            // We resolve as follows:
            // 1. Cached by-type lookup (fast path). This will work for almost all types after the very first resolution.
            // 2. Value-dependent type lookup (e.g. DateTime by Kind) via the resolvers. This includes complex types (e.g. array/range
            //    over DateTime), and the results cannot be cached.
            // 3. Uncached by-type lookup (for the very first resolution of a given type)

            var type = value.GetType();
            if (_handlersByClrType.TryGetValue(type, out var handler))
                return handler;

            foreach (var resolver in _resolvers)
            {
                try
                {
                    if ((handler = resolver.ResolveValueDependentValue(value)) is not null)
                        return handler;
                }
                catch (Exception e)
                {
                    Log.Error($"Type resolver {resolver.GetType().Name} threw exception while resolving value with type {type}", e);
                }
            }

            // ResolveByClrType either throws, or resolves a handler and caches it in _handlersByClrType (where it would be found above the
            // next time we resolve this type)
            return ResolveByClrType(type);
        }

        // TODO: This is needed as a separate method only because of binary COPY, see #3957
        internal OpenGaussTypeHandler ResolveByClrType(Type type)
        {
            if (_handlersByClrType.TryGetValue(type, out var handler))
                return handler;

            lock (_writeLock)
            {
                foreach (var resolver in _resolvers)
                {
                    try
                    {
                        if ((handler = resolver.ResolveByClrType(type)) is not null)
                            return _handlersByClrType[type] = handler;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Type resolver {resolver.GetType().Name} threw exception while resolving value with type {type}", e);
                    }
                }

                // Try to see if it is an array type
                var arrayElementType = GetArrayListElementType(type);
                if (arrayElementType is not null)
                {
                    // Arrays over range types are multiranges, not regular arrays.
                    if (arrayElementType.IsGenericType && arrayElementType.GetGenericTypeDefinition() == typeof(OpenGaussRange<>))
                    {
                        var subtypeType = arrayElementType.GetGenericArguments()[0];

                        return ResolveByClrType(subtypeType) is
                            { PostgresType : { Range : { Multirange: { } pgMultirangeType } } } subtypeHandler
                            ? _handlersByClrType[type] = subtypeHandler.CreateMultirangeHandler(pgMultirangeType)
                            : throw new NotSupportedException($"The CLR range type {type} isn't supported by OpenGauss or your PostgreSQL.");
                    }

                    if (ResolveByClrType(arrayElementType) is not { } elementHandler)
                        throw new ArgumentException($"Array type over CLR type {arrayElementType.Name} isn't supported by OpenGauss");

                    if (elementHandler.PostgresType.Array is not { } pgArrayType)
                        throw new ArgumentException(
                            $"No array type could be found in the database for element {elementHandler.PostgresType}");

                    return _handlersByClrType[type] =
                        elementHandler.CreateArrayHandler(pgArrayType, Connector.Settings.ArrayNullabilityMode);
                }

                if (Nullable.GetUnderlyingType(type) is { } underlyingType && ResolveByClrType(underlyingType) is { } underlyingHandler)
                    return _handlersByClrType[type] = underlyingHandler;

                if (type.IsEnum)
                {
                    return DatabaseInfo.GetPostgresTypeByName(GetPgName(type, DefaultNameTranslator)) is PostgresEnumType pgEnumType
                        ? _handlersByClrType[type] = new UnmappedEnumHandler(pgEnumType, DefaultNameTranslator, Connector.TextEncoding)
                        : throw new NotSupportedException(
                            $"Could not find a PostgreSQL enum type corresponding to {type.Name}. " +
                            "Consider mapping the enum before usage, refer to the documentation for more details.");
                }

                // TODO: We can make the following compatible with reflection-free mode by having OpenGaussRange implement some interface, and
                // check for that.
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(OpenGaussRange<>))
                {
                    var subtypeType = type.GetGenericArguments()[0];

                    return ResolveByClrType(subtypeType) is { PostgresType : { Range : { } pgRangeType } } subtypeHandler
                        ? _handlersByClrType[type] = subtypeHandler.CreateRangeHandler(pgRangeType)
                        : throw new NotSupportedException($"The CLR range type {type} isn't supported by OpenGauss or your PostgreSQL.");
                }

                if (typeof(IEnumerable).IsAssignableFrom(type))
                    throw new NotSupportedException("IEnumerable parameters are not supported, pass an array or List instead");

                throw new NotSupportedException($"The CLR type {type} isn't natively supported by OpenGauss or your PostgreSQL. " +
                                                $"To use it with a PostgreSQL composite you need to specify {nameof(OpenGaussParameter.DataTypeName)} or to map it, please refer to the documentation.");
            }

            static Type? GetArrayListElementType(Type type)
            {
                var typeInfo = type.GetTypeInfo();
                if (typeInfo.IsArray)
                    return GetUnderlyingType(type.GetElementType()!); // The use of bang operator is justified here as Type.GetElementType() only returns null for the Array base class which can't be mapped in a useful way.

                var ilist = typeInfo.ImplementedInterfaces.FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
                if (ilist != null)
                    return GetUnderlyingType(ilist.GetGenericArguments()[0]);

                if (typeof(IList).IsAssignableFrom(type))
                    throw new NotSupportedException("Non-generic IList is a supported parameter, but the OpenGaussDbType parameter must be set on the parameter");

                return null;

                Type GetUnderlyingType(Type t)
                    => Nullable.GetUnderlyingType(t) ?? t;
            }
        }

        internal bool TryGetMapping(PostgresType pgType, [NotNullWhen(true)] out TypeMappingInfo? mapping)
        {
            foreach (var resolver in _resolvers)
                if ((mapping = resolver.GetMappingByDataTypeName(pgType.Name)) is not null)
                    return true;

            switch (pgType)
            {
            case PostgresArrayType pgArrayType:
                if (TryGetMapping(pgArrayType.Element, out var elementMapping))
                {
                    mapping = new(elementMapping.OpenGaussDbType | OpenGaussDbType.Array, pgType.DisplayName);
                    return true;
                }

                break;

            case PostgresRangeType pgRangeType:
            {
                if (TryGetMapping(pgRangeType.Subtype, out var subtypeMapping))
                {
                    mapping = new(subtypeMapping.OpenGaussDbType | OpenGaussDbType.Range, pgType.DisplayName);
                    return true;
                }

                break;
            }

            case PostgresMultirangeType pgMultirangeType:
            {
                if (TryGetMapping(pgMultirangeType.Subrange.Subtype, out var subtypeMapping))
                {
                    mapping = new(subtypeMapping.OpenGaussDbType | OpenGaussDbType.Multirange, pgType.DisplayName);
                    return true;
                }

                break;
            }

            case PostgresDomainType pgDomainType:
                if (TryGetMapping(pgDomainType.BaseType, out var baseMapping))
                {
                    mapping = new(baseMapping.OpenGaussDbType, pgType.DisplayName, baseMapping.ClrTypes);
                    return true;
                }

                break;

            case PostgresEnumType or PostgresCompositeType:
                return _userTypeMappings.TryGetValue(pgType.OID, out mapping);
            }

            mapping = null;
            return false;
        }

        #endregion Type handler lookup

        #region Mapping management

        public override IOpenGaussTypeMapper MapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(TEnum), nameTranslator);

            if (DatabaseInfo.GetPostgresTypeByName(pgName) is not PostgresEnumType pgEnumType)
                throw new InvalidCastException($"Cannot map enum type {typeof(TEnum).Name} to PostgreSQL type {pgName} which isn't an enum");

            var handler = new UserEnumTypeMapping<TEnum>(pgName, nameTranslator).CreateHandler(pgEnumType, Connector);

            ApplyUserMapping(pgEnumType, typeof(TEnum), handler);

            return this;
        }

        public override bool UnmapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(TEnum), nameTranslator);

            var userEnumMapping = new UserEnumTypeMapping<TEnum>(pgName, nameTranslator);

            if (DatabaseInfo.GetPostgresTypeByName(pgName) is not PostgresEnumType pgEnumType)
                throw new InvalidCastException($"Could not find {pgName}");

            var found = _handlersByOID.TryRemove(pgEnumType.OID, out _);
            found |= _handlersByClrType.TryRemove(userEnumMapping.ClrType, out _);
            found |= _handlersByDataTypeName.TryRemove(userEnumMapping.PgTypeName, out _);
            return found;
        }

        public override IOpenGaussTypeMapper MapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(typeof(T), nameTranslator);

            if (DatabaseInfo.GetPostgresTypeByName(pgName) is not PostgresCompositeType pgCompositeType)
            {
                throw new InvalidCastException(
                    $"Cannot map composite type {typeof(T).Name} to PostgreSQL type {pgName} which isn't a composite");
            }

            var handler = new UserCompositeTypeMapping<T>(pgName, nameTranslator).CreateHandler(pgCompositeType, Connector);

            ApplyUserMapping(pgCompositeType, typeof(T), handler);

            return this;
        }

        public override IOpenGaussTypeMapper MapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(clrType, nameTranslator);

            if (DatabaseInfo.GetPostgresTypeByName(pgName) is not PostgresCompositeType pgCompositeType)
            {
                throw new InvalidCastException(
                    $"Cannot map composite type {clrType.Name} to PostgreSQL type {pgName} which isn't a composite");
            }

            var userCompositeMapping =
                (IUserTypeMapping)Activator.CreateInstance(typeof(UserCompositeTypeMapping<>).MakeGenericType(clrType),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new object[] { clrType, nameTranslator }, null)!;

            var handler = userCompositeMapping.CreateHandler(pgCompositeType, Connector);

            ApplyUserMapping(pgCompositeType, clrType, handler);

            return this;
        }

        public override bool UnmapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
            => UnmapComposite(typeof(T), pgName, nameTranslator);

        public override bool UnmapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
        {
            if (pgName != null && pgName.Trim() == "")
                throw new ArgumentException("pgName can't be empty", nameof(pgName));

            nameTranslator ??= DefaultNameTranslator;
            pgName ??= GetPgName(clrType, nameTranslator);

            if (DatabaseInfo.GetPostgresTypeByName(pgName) is not PostgresCompositeType pgCompositeType)
                throw new InvalidCastException($"Could not find {pgName}");

            var found = _handlersByOID.TryRemove(pgCompositeType.OID, out _);
            found |= _handlersByClrType.TryRemove(clrType, out _);
            found |= _handlersByDataTypeName.TryRemove(pgName, out _);
            return found;
        }

        void ApplyUserMapping(PostgresType pgType, Type clrType, OpenGaussTypeHandler handler)
        {
            _handlersByOID[pgType.OID] =
                _handlersByDataTypeName[pgType.FullName] =
                    _handlersByDataTypeName[pgType.Name] =
                        _handlersByClrType[clrType] = handler;

            _userTypeMappings[pgType.OID] = new(opengaussDbType: null, pgType.Name, clrType);
        }

        public override void AddTypeResolverFactory(TypeHandlerResolverFactory resolverFactory)
        {
            lock (this)
            {
                // Since EFCore.PG plugins (and possibly other users) repeatedly call OpenGaussConnection.GlobalTypeMapped.UseNodaTime,
                // we replace an existing resolver of the same CLR type.

                var newResolver = resolverFactory.Create(Connector);
                var newResolverType = newResolver.GetType();

                if (_resolvers[0].GetType() == newResolverType)
                    _resolvers[0] = newResolver;
                else
                {
                    for (var i = 0; i < _resolvers.Count; i++)
                        if (_resolvers[i].GetType() == newResolverType)
                            _resolvers.RemoveAt(i);

                    _resolvers.Insert(0, newResolver);
                }

                _handlersByOID.Clear();
                _handlersByOpenGaussDbType.Clear();
                _handlersByClrType.Clear();
                _handlersByDataTypeName.Clear();

                ChangeCounter = -1;
            }
        }

        public override void Reset()
        {
            lock (this)
            {
                var globalMapper = GlobalTypeMapper.Instance;
                globalMapper.Lock.EnterReadLock();
                try
                {
                    _handlersByOID.Clear();
                    _handlersByOpenGaussDbType.Clear();
                    _handlersByClrType.Clear();
                    _handlersByDataTypeName.Clear();

                    _resolvers.Clear();
                    for (var i = 0; i < globalMapper.ResolverFactories.Count; i++)
                        _resolvers.Add(globalMapper.ResolverFactories[i].Create(Connector));

                    _userTypeMappings.Clear();

                    foreach (var userTypeMapping in globalMapper.UserTypeMappings.Values)
                    {
                        if (DatabaseInfo.TryGetPostgresTypeByName(userTypeMapping.PgTypeName, out var pgType))
                        {
                            ApplyUserMapping(pgType, userTypeMapping.ClrType, userTypeMapping.CreateHandler(pgType, Connector));
                        }
                    }

                    ChangeCounter = GlobalTypeMapper.Instance.ChangeCounter;
                }
                finally
                {
                    globalMapper.Lock.ExitReadLock();
                }
            }
        }

        #endregion Mapping management

        internal (OpenGaussDbType? opengaussDbType, PostgresType postgresType) GetTypeInfoByOid(uint oid)
        {
            if (!DatabaseInfo.ByOID.TryGetValue(oid, out var pgType))
                throw new InvalidOperationException($"Couldn't find PostgreSQL type with OID {oid}");

            foreach (var resolver in _resolvers)
                if (resolver.GetMappingByDataTypeName(pgType.Name) is { } mapping)
                    return (mapping.OpenGaussDbType, pgType);

            switch (pgType)
            {
                case PostgresArrayType pgArrayType:
                    var (elementOpenGaussDbType, _) = GetTypeInfoByOid(pgArrayType.Element.OID);
                    if (elementOpenGaussDbType.HasValue)
                        return new(elementOpenGaussDbType | OpenGaussDbType.Array, pgType);
                    break;

                case PostgresDomainType pgDomainType:
                    var (baseOpenGaussDbType, _) = GetTypeInfoByOid(pgDomainType.BaseType.OID);
                    return new(baseOpenGaussDbType, pgType);
            }

            return (null, pgType);
        }
    }
}
