using System;
using OpenGauss.NET;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.TypeMapping;

namespace OpenGauss.GeoJSON.NET.Internal
{
    public class GeoJSONTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        readonly GeoJSONOptions _options;
        readonly bool _geographyAsDefault;

        public GeoJSONTypeHandlerResolverFactory(GeoJSONOptions options, bool geographyAsDefault)
            => (_options, _geographyAsDefault) = (options, geographyAsDefault);

        public override TypeHandlerResolver Create(OpenGaussConnector connector)
            => new GeoJSONTypeHandlerResolver(connector, _options, _geographyAsDefault);

        public override string? GetDataTypeNameByClrType(Type type)
            => GeoJSONTypeHandlerResolver.ClrTypeToDataTypeName(type, _geographyAsDefault);

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => GeoJSONTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
    }
}
