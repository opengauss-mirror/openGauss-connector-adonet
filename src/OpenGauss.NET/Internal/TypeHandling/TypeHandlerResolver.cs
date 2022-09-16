using System;

namespace OpenGauss.NET.Internal.TypeHandling
{
    /// <summary>
    /// An OpenGauss resolver for type handlers. Typically used by plugins to alter how OpenGauss reads and writes values to PostgreSQL.
    /// </summary>
    public abstract class TypeHandlerResolver
    {
        /// <summary>
        /// Resolves a type handler given a PostgreSQL type name, corresponding to the typname column in the PostgreSQL pg_type catalog table.
        /// </summary>
        /// <remarks>See <see href="https://www.postgresql.org/docs/current/catalog-pg-type.html" />.</remarks>
        public abstract OpenGaussTypeHandler? ResolveByDataTypeName(string typeName);

        /// <summary>
        /// Resolves a type handler given a .NET CLR type.
        /// </summary>
        public abstract OpenGaussTypeHandler? ResolveByClrType(Type type);

        public virtual OpenGaussTypeHandler? ResolveValueDependentValue(object value) => null;

        public virtual OpenGaussTypeHandler? ResolveValueTypeGenerically<T>(T value) => null;

        /// <summary>
        /// Gets type mapping information for a given PostgreSQL type.
        /// Invoked in scenarios when mapping information is required, rather than a type handler for reading or writing.
        /// </summary>
        public abstract TypeMappingInfo? GetMappingByDataTypeName(string dataTypeName);
    }
}
