using System;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;

namespace OpenGauss.NET.TypeMapping
{
    class BuiltInTypeHandlerResolverFactory : TypeHandlerResolverFactory
    {
        public override TypeHandlerResolver Create(OpenGaussConnector connector)
            => new BuiltInTypeHandlerResolver(connector);

        public override string? GetDataTypeNameByClrType(Type clrType)
            => BuiltInTypeHandlerResolver.ClrTypeToDataTypeName(clrType);

        public override string? GetDataTypeNameByValueDependentValue(object value)
            => BuiltInTypeHandlerResolver.ValueDependentValueToDataTypeName(value);

        public override TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName)
            => BuiltInTypeHandlerResolver.DoGetMappingByDataTypeName(dataTypeName);
    }
}
