using System;
using NUnit.Framework;
using NLog.Config;
using NLog.Targets;
using NLog;
using OpenGauss.NET.Logging;
using OpenGauss.Tests;
using OpenGauss.Tests.Support;

// ReSharper disable once CheckNamespace

[SetUpFixture]
public class LoggingSetupFixture
{
    [OneTimeSetUp]
    public void Setup()
    {
        var logLevelText = Environment.GetEnvironmentVariable("NPGSQL_TEST_LOGGING");
        if (logLevelText == null)
            return;
        if (!Enum.TryParse(logLevelText, true, out OpenGaussLogLevel logLevel))
            throw new ArgumentOutOfRangeException($"Invalid loglevel in NPGSQL_TEST_LOGGING: {logLevelText}");

        var config = new LoggingConfiguration();
        var consoleTarget = new ColoredConsoleTarget
        {
            Layout = @"${message} ${exception:format=tostring}"
        };
        config.AddTarget("console", consoleTarget);
        var rule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
        config.LoggingRules.Add(rule);
        LogManager.Configuration = config;

        OpenGaussLogManager.Provider = new NLogLoggingProvider();
        OpenGaussLogManager.IsParameterLoggingEnabled = true;
    }
}
