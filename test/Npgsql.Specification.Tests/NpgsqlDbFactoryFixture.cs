using System;
using System.Data.Common;
using AdoNet.Specification.Tests;

namespace Npgsql.Specification.Tests
{
    public class NpgsqlDbFactoryFixture : IDbFactoryFixture
    {
        public DbProviderFactory Factory => NpgsqlFactory.Instance;

        const string DefaultConnectionString =
            "Server=10.1.1.66;Port=15432;Username=gaussdb;Password=SZoscar55#;Database=postgres;Timeout=0;Command Timeout=0";

        public string ConnectionString =>
            Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") ?? DefaultConnectionString;
    }
}
