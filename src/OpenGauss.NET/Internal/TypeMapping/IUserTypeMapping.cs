using System;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeMapping
{
    public interface IUserTypeMapping
    {
        public string PgTypeName { get; }
        public Type ClrType { get; }

        public OpenGaussTypeHandler CreateHandler(PostgresType pgType, OpenGaussConnector connector);
    }
}
