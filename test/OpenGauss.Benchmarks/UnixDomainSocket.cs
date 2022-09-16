using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace OpenGauss.Benchmarks
{
    public class UnixDomainSocket
    {
        readonly OpenGaussConnection _tcpipConn;
        readonly OpenGaussCommand _tcpipCmd;
        readonly OpenGaussConnection _unixConn;
        readonly OpenGaussCommand _unixCmd;

        public UnixDomainSocket()
        {
            _tcpipConn = BenchmarkEnvironment.OpenConnection();
            _tcpipCmd = new OpenGaussCommand("SELECT @p", _tcpipConn);
            _tcpipCmd.Parameters.AddWithValue("p", new string('x', 10000));

            var port = new OpenGaussConnectionStringBuilder(BenchmarkEnvironment.ConnectionString).Port;
            var candidateDirectories = new[] { "/var/run/postgresql", "/tmp" };
            var dir = candidateDirectories.FirstOrDefault(d => File.Exists(Path.Combine(d, $".s.PGSQL.{port}")));
            if (dir == null)
                throw new Exception("No PostgreSQL unix domain socket was found");

            var connString = new OpenGaussConnectionStringBuilder(BenchmarkEnvironment.ConnectionString)
            {
                Host = dir
            }.ToString();
            _unixConn = new OpenGaussConnection(connString);
            _unixConn.Open();
            _unixCmd = new OpenGaussCommand("SELECT @p", _unixConn);
            _unixCmd.Parameters.AddWithValue("p", new string('x', 10000));
        }

        [Benchmark(Baseline = true)]
        public string Tcpip() => (string)_tcpipCmd.ExecuteScalar()!;

        [Benchmark]
        public string UnixDomain() => (string)_unixCmd.ExecuteScalar()!;
    }
}
