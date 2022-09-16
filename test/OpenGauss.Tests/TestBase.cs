using System;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET;

namespace OpenGauss.Tests
{
    public abstract class TestBase
    {
        /// <summary>
        /// The connection string that will be used when opening the connection to the tests database.
        /// May be overridden in fixtures, e.g. to set special connection parameters
        /// </summary>
        public virtual string ConnectionString => TestUtil.ConnectionString;

        static SemaphoreSlim DatabaseCreationLock = new(1);

        #region Utilities for use by tests

        protected virtual OpenGaussConnection CreateConnection(string? connectionString = null)
            => new(connectionString ?? ConnectionString);

        protected virtual OpenGaussConnection CreateConnection(Action<OpenGaussConnectionStringBuilder> builderAction)
        {
            var builder = new OpenGaussConnectionStringBuilder(ConnectionString);
            builderAction(builder);
            return new OpenGaussConnection(builder.ConnectionString);
        }

        protected virtual OpenGaussConnection OpenConnection(string? connectionString = null)
            => OpenConnection(connectionString, async: false).GetAwaiter().GetResult();

        protected virtual OpenGaussConnection OpenConnection(Action<OpenGaussConnectionStringBuilder> builderAction)
        {
            var builder = new OpenGaussConnectionStringBuilder(ConnectionString);
            builderAction(builder);
            return OpenConnection(builder.ConnectionString, async: false).GetAwaiter().GetResult();
        }

        protected virtual ValueTask<OpenGaussConnection> OpenConnectionAsync(string? connectionString = null)
            => OpenConnection(connectionString, async: true);

        protected virtual ValueTask<OpenGaussConnection> OpenConnectionAsync(
            Action<OpenGaussConnectionStringBuilder> builderAction)
        {
            var builder = new OpenGaussConnectionStringBuilder(ConnectionString);
            builderAction(builder);
            return OpenConnection(builder.ConnectionString, async: true);
        }

        ValueTask<OpenGaussConnection> OpenConnection(string? connectionString, bool async)
        {
            return OpenConnectionInternal(hasLock: false);

            async ValueTask<OpenGaussConnection> OpenConnectionInternal(bool hasLock)
            {
                var conn = CreateConnection(connectionString);
                try
                {
                    if (async)
                        await conn.OpenAsync();
                    else
                        conn.Open();
                    return conn;
                }
                catch (PostgresException e)
                {
                    if (e.SqlState == PostgresErrorCodes.InvalidPassword && connectionString == TestUtil.DefaultConnectionString)
                        throw new Exception("Please create a user opengauss_tests as follows: CREATE USER opengauss_tests PASSWORD 'opengauss_tests' SUPERUSER");

                    if (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
                    {
                        if (!hasLock)
                        {
                            DatabaseCreationLock.Wait();
                            try
                            {
                                return await OpenConnectionInternal(hasLock: true);
                            }
                            finally
                            {
                                DatabaseCreationLock.Release();
                            }
                        }

                        // Database does not exist and we have the lock, proceed to creation
                        var builder = new OpenGaussConnectionStringBuilder(connectionString ?? ConnectionString)
                        {
                            Pooling = false,
                            Multiplexing = false,
                            Database = "postgres"
                        };

                        using var adminConn = new OpenGaussConnection(builder.ConnectionString);
                        adminConn.Open();
                        adminConn.ExecuteNonQuery("CREATE DATABASE " + conn.Database);
                        adminConn.Close();
                        Thread.Sleep(1000);

                        if (async)
                            await conn.OpenAsync();
                        else
                            conn.Open();
                        return conn;
                    }

                    throw;
                }
            }
        }

        protected OpenGaussConnection OpenConnection(OpenGaussConnectionStringBuilder csb)
            => OpenConnection(csb.ToString());

        protected virtual ValueTask<OpenGaussConnection> OpenConnectionAsync(OpenGaussConnectionStringBuilder csb)
            => OpenConnectionAsync(csb.ToString());

        // In PG under 9.1 you can't do SELECT pg_sleep(2) in binary because that function returns void and PG doesn't know
        // how to transfer that. So cast to text server-side.
        protected static OpenGaussCommand CreateSleepCommand(OpenGaussConnection conn, int seconds = 1000)
            => new($"SELECT pg_sleep({seconds}){(conn.PostgreSqlVersion < new Version(9, 1, 0) ? "::TEXT" : "")}", conn);

        protected bool IsRedshift => new OpenGaussConnectionStringBuilder(ConnectionString).ServerCompatibilityMode == ServerCompatibilityMode.Redshift;

        #endregion
    }
}
