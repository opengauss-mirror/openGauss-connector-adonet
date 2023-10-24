using System;
using System.Data.Common;
using AdoNet.Specification.Tests;

namespace OpenGauss.Specification.Tests
{
    public class OpenGaussDbFactoryFixture : IDbFactoryFixture
    {
        public DbProviderFactory Factory => OpenGaussFactory.Instance;

        const string DefaultConnectionString =
            "Server=10.1.1.66;Port=15432;Username=gaussdb;Password=SZoscar55#;Database=postgres;Timeout=0;Command Timeout=0";

        public string ConnectionString =>
            Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") ?? DefaultConnectionString;
    }
}
