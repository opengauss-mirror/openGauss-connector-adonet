using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NetTopologySuite.Internal
{
    public class NetTopologySuiteTypeHandlerResolver : TypeHandlerResolver
    {
        readonly OpenGaussDatabaseInfo _databaseInfo;
        readonly bool _geographyAsDefault;

        readonly NetTopologySuiteHandler? _geometryHandler, _geographyHandler;

        internal NetTopologySuiteTypeHandlerResolver(
            OpenGaussConnector connector,
            CoordinateSequenceFactory coordinateSequenceFactory,
            PrecisionModel precisionModel,
            Ordinates handleOrdinates,
            bool geographyAsDefault)
        {
            _databaseInfo = connector.DatabaseInfo;
            _geographyAsDefault = geographyAsDefault;

            var (pgGeometryType, pgGeographyType) = (PgType("geometry"), PgType("geography"));

            // TODO: In multiplexing, these are used concurrently... not sure they're thread-safe :(
            var reader = new PostGisReader(coordinateSequenceFactory, precisionModel, handleOrdinates);
            var writer = new PostGisWriter();

            if (pgGeometryType is not null)
                _geometryHandler = new NetTopologySuiteHandler(pgGeometryType, reader, writer);
            if (pgGeographyType is not null)
                _geographyHandler = new NetTopologySuiteHandler(pgGeographyType, reader, writer);
        }

        public override OpenGaussTypeHandler? ResolveByDataTypeName(string typeName)
            => typeName switch
            {
                "geometry" => _geometryHandler,
                "geography" => _geographyHandler,
                _ => null
            };

        public override OpenGaussTypeHandler? ResolveByClrType(Type type)
            => ClrTypeToDataTypeName(type, _geographyAsDefault) is { } dataTypeName && ResolveByDataTypeName(dataTypeName) is { } handler
                ? handler
                : null;

        internal static string? ClrTypeToDataTypeName(Type type, bool geographyAsDefault)
            => type != typeof(Geometry) && type.BaseType != typeof(Geometry) && type.BaseType != typeof(GeometryCollection)
                ? null
                : geographyAsDefault
                    ? "geography"
                    : "geometry";

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => DoGetMappingByDataTypeName(dataTypeName);

        internal static TypeMappingInfo? DoGetMappingByDataTypeName(string dataTypeName)
            => dataTypeName switch
            {
                "geometry"  => new(OpenGaussDbType.Geometry,  "geometry"),
                "geography" => new(OpenGaussDbType.Geography, "geography"),
                _ => null
            };

        PostgresType? PgType(string pgTypeName) => _databaseInfo.TryGetPostgresTypeByName(pgTypeName, out var pgType) ? pgType : null;
    }
}
