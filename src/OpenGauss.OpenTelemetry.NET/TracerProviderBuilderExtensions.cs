using System;
using OpenTelemetry.Trace;

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET
{
    /// <summary>
    /// Extension method for setting up OpenGauss OpenTelemetry tracing.
    /// </summary>
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Subscribes to the OpenGauss activity source to enable OpenTelemetry tracing.
        /// </summary>
        public static TracerProviderBuilder AddOpenGauss(
            this TracerProviderBuilder builder,
            Action<OpenGaussTracingOptions>? options = null)
            => builder.AddSource("OpenGauss");
    }
}
