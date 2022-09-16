using System;
using NLog;
using OpenGauss.NET.Logging;

namespace OpenGauss.Tests.Support
{
    class NLogLoggingProvider : IOpenGaussLoggingProvider
    {
        public OpenGaussLogger CreateLogger(string name)
        {
            return new NLogLogger(name);
        }
    }

    class NLogLogger : OpenGaussLogger
    {
        readonly Logger _log;

        internal NLogLogger(string name)
        {
            _log = LogManager.GetLogger(name);
        }

        public override bool IsEnabled(OpenGaussLogLevel level)
        {
            return _log.IsEnabled(ToNLogLogLevel(level));
        }

        public override void Log(OpenGaussLogLevel level, int connectorId, string msg, Exception? exception = null)
        {
            var ev = new LogEventInfo(ToNLogLogLevel(level), "", msg);
            if (exception != null)
                ev.Exception = exception;
            if (connectorId != 0)
                ev.Properties["ConnectorId"] = connectorId;
            _log.Log(ev);
        }

        static LogLevel ToNLogLogLevel(OpenGaussLogLevel level)
            => level switch
            {
                OpenGaussLogLevel.Trace => LogLevel.Trace,
                OpenGaussLogLevel.Debug => LogLevel.Debug,
                OpenGaussLogLevel.Info  => LogLevel.Info,
                OpenGaussLogLevel.Warn  => LogLevel.Warn,
                OpenGaussLogLevel.Error => LogLevel.Error,
                OpenGaussLogLevel.Fatal => LogLevel.Fatal,
                _                    => throw new ArgumentOutOfRangeException(nameof(level))
            };
    }
}
