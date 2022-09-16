using OpenGauss.NodaTime.NET.Internal;
using OpenGauss.NET.TypeMapping;

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET
{
    /// <summary>
    /// Extension adding the NodaTime plugin to an OpenGauss type mapper.
    /// </summary>
    public static class OpenGaussNodaTimeExtensions
    {
        /// <summary>
        /// Sets up NodaTime mappings for the PostgreSQL date/time types.
        /// </summary>
        /// <param name="mapper">The type mapper to set up (global or connection-specific)</param>
        public static IOpenGaussTypeMapper UseNodaTime(this IOpenGaussTypeMapper mapper)
        {
            mapper.AddTypeResolverFactory(new NodaTimeTypeHandlerResolverFactory());
            return mapper;
        }
    }
}
