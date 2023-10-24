using System;
using OpenGauss.NET.Internal.TypeHandlers.CompositeHandlers;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeMapping
{
    public interface IUserCompositeTypeMapping : IUserTypeMapping
    {
        IOpenGaussNameTranslator NameTranslator { get; }
    }

    class UserCompositeTypeMapping<T> : IUserCompositeTypeMapping
    {
        public string PgTypeName { get; }
        public Type ClrType => typeof(T);
        public IOpenGaussNameTranslator NameTranslator { get; }

        public UserCompositeTypeMapping(string pgTypeName, IOpenGaussNameTranslator nameTranslator)
            => (PgTypeName, NameTranslator) = (pgTypeName, nameTranslator);

        public OpenGaussTypeHandler CreateHandler(PostgresType pgType, OpenGaussConnector connector)
            => new CompositeHandler<T>((PostgresCompositeType)pgType, connector.TypeMapper, NameTranslator);
    }
}
