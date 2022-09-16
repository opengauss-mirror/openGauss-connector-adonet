using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.TypeMapping
{
    abstract class TypeMapperBase : IOpenGaussTypeMapper
    {
        public IOpenGaussNameTranslator DefaultNameTranslator { get; }

        protected TypeMapperBase(IOpenGaussNameTranslator defaultNameTranslator)
        {
            if (defaultNameTranslator == null)
                throw new ArgumentNullException(nameof(defaultNameTranslator));

            DefaultNameTranslator = defaultNameTranslator;
        }

        #region Mapping management

        /// <inheritdoc />
        public abstract IOpenGaussTypeMapper MapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
            where TEnum : struct, Enum;

        /// <inheritdoc />
        public abstract bool UnmapEnum<TEnum>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null)
            where TEnum : struct, Enum;

        /// <inheritdoc />
        [RequiresUnreferencedCode("Composite type mapping currently isn't trimming-safe.")]
        public abstract IOpenGaussTypeMapper MapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null);

        /// <inheritdoc />
        [RequiresUnreferencedCode("Composite type mapping currently isn't trimming-safe.")]
        public abstract IOpenGaussTypeMapper MapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null);

        /// <inheritdoc />
        public abstract bool UnmapComposite<T>(string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null);

        /// <inheritdoc />
        public abstract bool UnmapComposite(Type clrType, string? pgName = null, IOpenGaussNameTranslator? nameTranslator = null);

        /// <inheritdoc />
        public abstract void AddTypeResolverFactory(TypeHandlerResolverFactory resolverFactory);

        public abstract void Reset();

        #endregion Composite mapping

        #region Misc

        private protected static string GetPgName(Type clrType, IOpenGaussNameTranslator nameTranslator)
            => clrType.GetCustomAttribute<PgNameAttribute>()?.PgName
               ?? nameTranslator.TranslateTypeName(clrType.Name);

        #endregion Misc
    }
}
