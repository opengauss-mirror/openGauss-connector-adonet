using System;

namespace OpenGauss.Benchmarks
{
    static class BenchmarkEnvironment
    {
        internal static string ConnectionString => Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") ?? DefaultConnectionString;

        /// <summary>
        /// Unless the NPGSQL_TEST_DB environment variable is defined, this is used as the connection string for the
        /// test database.
        /// </summary>
        const string DefaultConnectionString = "Server=10.1.1.66;Port=15432;Username=gaussdb;Password=SZoscar55#;Database=postgres;";

        internal static OpenGaussConnection GetConnection() => new(ConnectionString);

        internal static OpenGaussConnection OpenConnection()
        {
            var conn = GetConnection();
            conn.Open();
            return conn;
        } 
    }
}
