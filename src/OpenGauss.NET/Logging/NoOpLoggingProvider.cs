using System;

namespace OpenGauss.NET.Logging
{
    class NoOpLoggingProvider : IOpenGaussLoggingProvider
    {
        public OpenGaussLogger CreateLogger(string name) => NoOpLogger.Instance;
    }

    class NoOpLogger : OpenGaussLogger
    {
        internal static NoOpLogger Instance = new();

        NoOpLogger() {}
        public override bool IsEnabled(OpenGaussLogLevel level) => false;
        public override void Log(OpenGaussLogLevel level, int connectorId, string msg, Exception? exception = null)
        {
        }
    }
}
