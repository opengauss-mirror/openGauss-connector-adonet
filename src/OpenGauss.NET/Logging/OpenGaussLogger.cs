using System;

#pragma warning disable 1591

namespace OpenGauss.NET.Logging
{
    /// <summary>
    /// A generic interface for logging.
    /// </summary>
    public abstract class OpenGaussLogger
    {
        public abstract bool IsEnabled(OpenGaussLogLevel level);
        public abstract void Log(OpenGaussLogLevel level, int connectorId, string msg, Exception? exception = null);

        internal void Trace(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Trace, connectionId, msg);
        internal void Debug(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Debug, connectionId, msg);
        internal void Info(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Info, connectionId, msg);
        internal void Warn(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Warn, connectionId, msg);
        internal void Error(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Error, connectionId, msg);
        internal void Fatal(string msg, int connectionId = 0) => Log(OpenGaussLogLevel.Fatal, connectionId, msg);

        internal void Trace(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Trace, connectionId, msg, ex);
        internal void Debug(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Debug, connectionId, msg, ex);
        internal void Info(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Info, connectionId, msg, ex);
        internal void Warn(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Warn, connectionId, msg, ex);
        internal void Error(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Error, connectionId, msg, ex);
        internal void Fatal(string msg, Exception ex, int connectionId = 0) => Log(OpenGaussLogLevel.Fatal, connectionId, msg, ex);
    }
}
