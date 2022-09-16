using System;
using System.Data;
using OpenGauss.NET.TypeMapping;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandling
{
    public class TypeMappingInfo
    {
        public TypeMappingInfo(OpenGaussDbType? opengaussDbType, string? dataTypeName, Type clrType)
            => (OpenGaussDbType, DataTypeName, ClrTypes) = (opengaussDbType, dataTypeName, new[] { clrType });

        public TypeMappingInfo(OpenGaussDbType? opengaussDbType, string? dataTypeName, params Type[] clrTypes)
            => (OpenGaussDbType, DataTypeName, ClrTypes) = (opengaussDbType, dataTypeName, clrTypes);

        public OpenGaussDbType? OpenGaussDbType { get; }
        DbType? dbType;
        public DbType DbType
            => dbType ??= OpenGaussDbType is null ? DbType.Object : GlobalTypeMapper.OpenGaussDbTypeToDbType(OpenGaussDbType.Value);
        public string? DataTypeName { get; }
        public Type[] ClrTypes { get; }

        internal void Reset() => dbType = null;
    }
}
