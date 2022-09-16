using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.TypeMapping;
using OpenGauss.NET.Types;

namespace OpenGauss.Json.NET.Internal
{
    public class JsonNetTypeHandlerResolver : TypeHandlerResolver
    {
        readonly OpenGaussDatabaseInfo _databaseInfo;
        readonly JsonbHandler _jsonbHandler;
        readonly JsonHandler _jsonHandler;
        readonly Dictionary<Type, string> _dataTypeNamesByClrType;

        internal JsonNetTypeHandlerResolver(
            OpenGaussConnector connector,
            Dictionary<Type, string> dataClrTypeNamesDataTypeNamesByClrClrType,
            JsonSerializerSettings settings)
        {
            _databaseInfo = connector.DatabaseInfo;

            _jsonbHandler = new JsonbHandler(PgType("jsonb"), connector, settings);
            _jsonHandler = new JsonHandler(PgType("json"), connector, settings);

            _dataTypeNamesByClrType = dataClrTypeNamesDataTypeNamesByClrClrType;
        }

        public OpenGaussTypeHandler? ResolveOpenGaussDbType(OpenGaussDbType opengaussDbType)
            => opengaussDbType switch
            {
                OpenGaussDbType.Jsonb => _jsonbHandler,
                OpenGaussDbType.Json => _jsonHandler,
                _ => null
            };

        public override OpenGaussTypeHandler? ResolveByDataTypeName(string typeName)
            => typeName switch
            {
                "jsonb" => _jsonbHandler,
                "json" => _jsonHandler,
                _ => null
            };

        public override OpenGaussTypeHandler? ResolveByClrType(Type type)
            => ClrTypeToDataTypeName(type, _dataTypeNamesByClrType) is { } dataTypeName && ResolveByDataTypeName(dataTypeName) is { } handler
                ? handler
                : null;

        internal static string? ClrTypeToDataTypeName(Type type, Dictionary<Type, string> clrTypes)
            => clrTypes.TryGetValue(type, out var dataTypeName) ? dataTypeName : null;

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => DoGetMappingByDataTypeName(dataTypeName);

        internal static TypeMappingInfo? DoGetMappingByDataTypeName(string dataTypeName)
            => dataTypeName switch
            {
                "jsonb" => new(OpenGaussDbType.Jsonb,   "jsonb"),
                "json"  => new(OpenGaussDbType.Json,    "json"),
                _ => null
            };

        PostgresType PgType(string pgTypeName) => _databaseInfo.GetPostgresTypeByName(pgTypeName);
    }
}
