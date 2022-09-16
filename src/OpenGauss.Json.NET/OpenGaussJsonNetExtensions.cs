using System;
using OpenGauss.NET.TypeMapping;
using OpenGauss.NET.Types;
using Newtonsoft.Json;
using OpenGauss.Json.NET.Internal;

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET
{
    /// <summary>
    /// Extension allowing adding the Json.NET plugin to an OpenGauss type mapper.
    /// </summary>
    public static class OpenGaussJsonNetExtensions
    {
        /// <summary>
        /// Sets up JSON.NET mappings for the PostgreSQL json and jsonb types.
        /// </summary>
        /// <param name="mapper">The type mapper to set up (global or connection-specific)</param>
        /// <param name="jsonbClrTypes">A list of CLR types to map to PostgreSQL jsonb (no need to specify OpenGaussDbType.Jsonb)</param>
        /// <param name="jsonClrTypes">A list of CLR types to map to PostgreSQL json (no need to specify OpenGaussDbType.Json)</param>
        /// <param name="settings">Optional settings to customize JSON serialization</param>
        public static IOpenGaussTypeMapper UseJsonNet(
            this IOpenGaussTypeMapper mapper,
            Type[]? jsonbClrTypes = null,
            Type[]? jsonClrTypes = null,
            JsonSerializerSettings? settings = null)
        {
            mapper.AddTypeResolverFactory(new JsonNetTypeHandlerResolverFactory(jsonbClrTypes, jsonClrTypes, settings));
            return mapper;
        }
    }
}
