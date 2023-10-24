using System;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;

namespace OpenGauss.NodaTime.NET.Internal
{
    public class NodaTimeTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        public override TypeHandlerResolver Create(OpenGaussConnector connector)
            => new NodaTimeTypeHandlerResolver(connector);

        public override string? GetDataTypeNameByClrType(Type type)
            => NodaTimeTypeHandlerResolver.ClrTypeToDataTypeName(type);

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => NodaTimeTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
    }
}
