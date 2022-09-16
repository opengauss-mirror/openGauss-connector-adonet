using System;

namespace OpenGauss.NET.Logging
{
    /// <summary>
    /// Manages logging for OpenGauss, used to set the logging provider.
    /// </summary>
    public static class OpenGaussLogManager
    {
        /// <summary>
        /// The logging provider used for logging in OpenGauss.
        /// </summary>
        public static IOpenGaussLoggingProvider Provider
        {
            get
            {
                _providerRetrieved = true;
                return _provider;
            }
            set
            {
                if (_providerRetrieved)
                    throw new InvalidOperationException("The logging provider must be set before any OpenGauss action is taken");

                _provider = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Determines whether parameter contents will be logged alongside SQL statements - this may reveal sensitive information.
        /// Defaults to false.
        /// </summary>
        public static bool IsParameterLoggingEnabled { get; set; }

        static IOpenGaussLoggingProvider _provider = new NoOpLoggingProvider();
        static bool _providerRetrieved;

        internal static OpenGaussLogger CreateLogger(string name) => Provider.CreateLogger("OpenGauss.NET." + name);
    }
}
