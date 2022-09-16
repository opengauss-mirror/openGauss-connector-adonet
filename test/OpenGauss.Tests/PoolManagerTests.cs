using NUnit.Framework;
using OpenGauss.NET;

namespace OpenGauss.Tests
{
    [NonParallelizable]
    class PoolManagerTests : TestBase
    {
        [Test]
        public void With_canonical_connection_string()
        {
            var connString = new OpenGaussConnectionStringBuilder(ConnectionString).ToString();
            using (var conn = new OpenGaussConnection(connString))
                conn.Open();
            var connString2 = new OpenGaussConnectionStringBuilder(ConnectionString)
            {
                ApplicationName = "Another connstring"
            }.ToString();
            using (var conn = new OpenGaussConnection(connString2))
                conn.Open();
        }

#if DEBUG
        [Test]
        public void Many_pools()
        {
            PoolManager.Reset();
            for (var i = 0; i < PoolManager.InitialPoolsSize + 1; i++)
            {
                var connString = new OpenGaussConnectionStringBuilder(ConnectionString)
                {
                    ApplicationName = "App" + i
                }.ToString();
                using var conn = new OpenGaussConnection(connString);
                conn.Open();
            }
            PoolManager.Reset();
        }
#endif

        [Test]
        public void ClearAllPools()
        {
            using (OpenConnection()) {}
            // Now have one connection in the pool
            Assert.That(PoolManager.TryGetValue(ConnectionString, out var pool), Is.True);
            Assert.That(pool!.Statistics.Idle, Is.EqualTo(1));

            OpenGaussConnection.ClearAllPools();
            Assert.That(pool.Statistics.Idle, Is.Zero);
            Assert.That(pool.Statistics.Total, Is.Zero);
        }

        [Test]
        public void ClearAllPools_with_busy()
        {
            ConnectorSource? pool;
            using (OpenConnection())
            {
                using (OpenConnection()) { }
                // We have one idle, one busy

                OpenGaussConnection.ClearAllPools();
                Assert.That(PoolManager.TryGetValue(ConnectionString, out pool), Is.True);
                Assert.That(pool!.Statistics.Idle, Is.Zero);
                Assert.That(pool.Statistics.Total, Is.EqualTo(1));
            }
            Assert.That(pool.Statistics.Idle, Is.Zero);
            Assert.That(pool.Statistics.Total, Is.Zero);
        }

        [SetUp]
        public void Setup() => PoolManager.Reset();

        [TearDown]
        public void Teardown() => PoolManager.Reset();
    }
}
