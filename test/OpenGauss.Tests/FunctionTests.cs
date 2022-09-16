using System;
using System.Data;
using NUnit.Framework;
using OpenGauss.NET;

namespace OpenGauss.Tests
{
    /// <summary>
    /// A fixture for tests which interact with functions.
    /// All tests should create functions in the pg_temp schema only to ensure there's no interaction between
    /// the tests.
    /// </summary>
    public class FunctionTests : TestBase
    {
        [Test, Description("Simple function with no parameters, results accessed as a resultset")]
        public void Resultset()
        {
            using var conn = OpenConnection();
            conn.ExecuteNonQuery(@"CREATE FUNCTION pg_temp.func() RETURNS integer AS 'SELECT 8;' LANGUAGE 'sql'");
            using var cmd = new OpenGaussCommand("pg_temp.func", conn) { CommandType = CommandType.StoredProcedure };
            Assert.That(cmd.ExecuteScalar(), Is.EqualTo(8));
        }

        [Test, Description("Basic function call with an in parameter")]
        public void Param_Input()
        {
            using var conn = OpenConnection();
            conn.ExecuteNonQuery(@"CREATE FUNCTION pg_temp.echo(IN param text) RETURNS text AS 'BEGIN RETURN param; END;' LANGUAGE 'plpgsql'");
            using var cmd = new OpenGaussCommand("pg_temp.echo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@param", "hello");
            Assert.That(cmd.ExecuteScalar(), Is.EqualTo("hello"));
            conn.ExecuteNonQuery(@"DROP FUNCTION pg_temp.echo(IN param text)");
        }

        [Test, Description("Basic function call with an out parameter")]
        public void Param_Output()
        {
            using var conn = OpenConnection();
            
            conn.ExecuteNonQuery(@"CREATE FUNCTION pg_temp.echo (IN param_in text, OUT param_out text) AS 'BEGIN param_out=param_in; END;' LANGUAGE 'plpgsql'");
            using var cmd = new OpenGaussCommand("pg_temp.echo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@param_in", "hello");
            var outParam = new OpenGaussParameter("param_out", DbType.String) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            cmd.ExecuteNonQuery();
            Assert.That(outParam.Value, Is.EqualTo("hello"));
            conn.ExecuteNonQuery(@"DROP FUNCTION pg_temp.echo(IN param_in text, OUT param_out text)");
        }

        [Test, Description("Basic function call with an in/out parameter")]
        public void Param_InputOutput()
        {
            using var conn = OpenConnection();
            conn.ExecuteNonQuery(@"CREATE FUNCTION pg_temp.inc (INOUT param integer) AS 'BEGIN param=param+1; END;' LANGUAGE 'plpgsql'");
            using var cmd = new OpenGaussCommand("pg_temp.inc", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            var outParam = new OpenGaussParameter("param", DbType.Int32)
            {
                Direction = ParameterDirection.InputOutput,
                Value = 8
            };
            cmd.Parameters.Add(outParam);
            cmd.ExecuteNonQuery();
            Assert.That(outParam.Value, Is.EqualTo(9));
        }

        [Test]
        public void Void()
        {
            using var conn = OpenConnection();
            TestUtil.MinimumPgVersion(conn, "9.1.0", "no binary output function available for type void before 9.1.0");
            var command = new OpenGaussCommand("pg_sleep", conn);
            command.Parameters.AddWithValue(0);
            command.CommandType = CommandType.StoredProcedure;
            command.ExecuteNonQuery();
        }

        [Test]
        public void Named_parameters()
        {
            using var conn = OpenConnection();
            TestUtil.MinimumPgVersion(conn, "9.4.0", "make_timestamp was introduced in 9.4");
            using var command = new OpenGaussCommand("make_timestamp", conn);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("year", 2015);
            command.Parameters.AddWithValue("month", 8);
            command.Parameters.AddWithValue("mday", 1);
            command.Parameters.AddWithValue("hour", 2);
            command.Parameters.AddWithValue("min", 3);
            command.Parameters.AddWithValue("sec", 4);
            var dt = (DateTime) command.ExecuteScalar()!;

            Assert.AreEqual(new DateTime(2015, 8, 1, 2, 3, 4), dt);

            command.Parameters[0].Value = 2014;
            command.Parameters[0].ParameterName = ""; // 2014 will be sent as a positional parameter
            dt = (DateTime) command.ExecuteScalar()!;
            Assert.AreEqual(new DateTime(2014, 8, 1, 2, 3, 4), dt);
        }

        [Test]
        public void Too_many_output_params()
        {
            using var conn = OpenConnection();
            var command = new OpenGaussCommand("VALUES (4,5), (6,7)", conn);
            command.Parameters.Add(new OpenGaussParameter("a", DbType.Int32)
            {
                Direction = ParameterDirection.Output,
                Value = -1
            });
            command.Parameters.Add(new OpenGaussParameter("b", DbType.Int32)
            {
                Direction = ParameterDirection.Output,
                Value = -1
            });
            command.Parameters.Add(new OpenGaussParameter("c", DbType.Int32)
            {
                Direction = ParameterDirection.Output,
                Value = -1
            });

            command.ExecuteNonQuery();

            Assert.That(command.Parameters["a"].Value, Is.EqualTo(4));
            Assert.That(command.Parameters["b"].Value, Is.EqualTo(5));
            Assert.That(command.Parameters["c"].Value, Is.EqualTo(-1));
        }
    }
}
