using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Microsoft.Data.SqlClient;

// ReSharper disable UnusedMember.Global

namespace OpenGauss.Benchmarks
{
    [Config(typeof(Config))]
    public class ConnectionCreationBenchmarks
    {
        const string OpenGaussConnectionString = "Host=foo;Database=bar;Username=user;Password=password";
        const string SqlClientConnectionString = @"Data Source=(localdb)\mssqllocaldb";

        [Benchmark]
        public OpenGaussConnection OpenGauss() => new(OpenGaussConnectionString);

        [Benchmark]
        public SqlConnection SqlClient() => new(SqlClientConnectionString);

        class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(StatisticColumn.OperationsPerSecond);
            }
        }
    }
}
