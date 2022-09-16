using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using OpenGauss.NET.Types;
using NUnit.Framework;
using static OpenGauss.Tests.TestUtil;
using OpenGauss.NET;

namespace OpenGauss.Tests.Types
{
    /// <summary>
    /// Tests on PostgreSQL numeric types
    /// </summary>
    /// <summary>
    /// https://www.postgresql.org/docs/current/static/datatype-numeric.html
    /// </summary>
    public class NumericTypeTests : MultiplexingTestBase
    {
        [Test]
        public async Task Int16()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3, @p4, @p5", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Smallint);
            var p2 = new OpenGaussParameter("p2", DbType.Int16);
            var p3 = new OpenGaussParameter("p3", DbType.Byte);
            var p4 = new OpenGaussParameter { ParameterName = "p4", Value = (short)8 };
            var p5 = new OpenGaussParameter { ParameterName = "p5", Value = (byte)8  };
            Assert.That(p4.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Smallint));
            Assert.That(p4.DbType, Is.EqualTo(DbType.Int16));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            cmd.Parameters.Add(p4);
            cmd.Parameters.Add(p5);
            p1.Value = p2.Value = p3.Value = (long)8;
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetInt16(i), Is.EqualTo(8));
                Assert.That(reader.GetInt32(i), Is.EqualTo(8));
                Assert.That(reader.GetInt64(i), Is.EqualTo(8));
                Assert.That(reader.GetByte(i), Is.EqualTo(8));
                Assert.That(reader.GetFloat(i), Is.EqualTo(8.0f));
                Assert.That(reader.GetDouble(i), Is.EqualTo(8.0d));
                Assert.That(reader.GetDecimal(i), Is.EqualTo(8.0m));
                Assert.That(reader.GetValue(i), Is.EqualTo(8));
                Assert.That(reader.GetProviderSpecificValue(i), Is.EqualTo(8));
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(short)));
                Assert.That(reader.GetDataTypeName(i), Is.EqualTo("smallint"));
            }
        }

        [Test]
        public async Task Int32()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Integer);
            var p2 = new OpenGaussParameter("p2", DbType.Int32);
            var p3 = new OpenGaussParameter { ParameterName = "p3", Value = 8 };
            Assert.That(p3.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Integer));
            Assert.That(p3.DbType, Is.EqualTo(DbType.Int32));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            p1.Value = p2.Value = (long)8;
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetInt32(i),                 Is.EqualTo(8));
                Assert.That(reader.GetInt64(i),                 Is.EqualTo(8));
                Assert.That(reader.GetInt16(i),                 Is.EqualTo(8));
                Assert.That(reader.GetByte(i),                  Is.EqualTo(8));
                Assert.That(reader.GetFloat(i),                 Is.EqualTo(8.0f));
                Assert.That(reader.GetDouble(i),                Is.EqualTo(8.0d));
                Assert.That(reader.GetDecimal(i),               Is.EqualTo(8.0m));
                Assert.That(reader.GetValue(i),                 Is.EqualTo(8));
                Assert.That(reader.GetProviderSpecificValue(i), Is.EqualTo(8));
                Assert.That(reader.GetFieldType(i),             Is.EqualTo(typeof(int)));
                Assert.That(reader.GetDataTypeName(i),          Is.EqualTo("integer"));
            }
        }

        [Test, Description("Tests some types which are aliased to UInt32")]
        [TestCase(OpenGaussDbType.Oid, TestName="OID")]
        //[TestCase(OpenGaussDbType.Xid, TestName="XID")]
        [TestCase(OpenGaussDbType.Cid, TestName="CID")]
        public async Task UInt32(OpenGaussDbType opengaussDbType)
        {
            var expected = 8u;
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p", conn);
            cmd.Parameters.Add(new OpenGaussParameter("p", opengaussDbType) { Value = expected });
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            Assert.That(reader[0], Is.EqualTo(expected));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(expected));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(uint)));
        }

        //[Test]
        //[TestCase(OpenGaussDbType.Xid8, TestName="XID8")]
        public async Task UInt64(OpenGaussDbType opengaussDbType)
        {
            await using var conn = await OpenConnectionAsync();
            MinimumPgVersion(conn, "13.0", "The xid8 type was introduced in PostgreSQL 13");

            var expected = 8ul;
            await using var cmd = new OpenGaussCommand("SELECT @p", conn);
            cmd.Parameters.Add(new OpenGaussParameter("p", opengaussDbType) { Value = expected });
            await using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            Assert.That(reader[0], Is.EqualTo(expected));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(expected));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(ulong)));
        }

        [Test]
        public async Task Int64()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Bigint);
            var p2 = new OpenGaussParameter("p2", DbType.Int64);
            var p3 = new OpenGaussParameter { ParameterName = "p3", Value = (long)8 };
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            p1.Value = p2.Value = (short)8;
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetInt64(i),                 Is.EqualTo(8));
                Assert.That(reader.GetInt16(i),                 Is.EqualTo(8));
                Assert.That(reader.GetInt32(i),                 Is.EqualTo(8));
                Assert.That(reader.GetByte(i),                  Is.EqualTo(8));
                Assert.That(reader.GetFloat(i),                 Is.EqualTo(8.0f));
                Assert.That(reader.GetDouble(i),                Is.EqualTo(8.0d));
                Assert.That(reader.GetDecimal(i),               Is.EqualTo(8.0m));
                Assert.That(reader.GetValue(i),                 Is.EqualTo(8));
                Assert.That(reader.GetProviderSpecificValue(i), Is.EqualTo(8));
                Assert.That(reader.GetFieldType(i),             Is.EqualTo(typeof(long)));
                Assert.That(reader.GetDataTypeName(i),          Is.EqualTo("bigint"));
            }
        }

        [Test]
        public async Task Double()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3", conn);
            const double expected = 4.123456789012345;
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Double);
            var p2 = new OpenGaussParameter("p2", DbType.Double);
            var p3 = new OpenGaussParameter {ParameterName = "p3", Value = expected};
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            p1.Value = p2.Value = expected;
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetDouble(i), Is.EqualTo(expected).Within(10E-07));
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(double)));
            }
        }

        [Test]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public async Task Double_special_values(double value)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p", conn);
            cmd.Parameters.AddWithValue("p", OpenGaussDbType.Double, value);
            var actual = await cmd.ExecuteScalarAsync();
            Assert.That(actual, Is.EqualTo(value));
        }

        [Test]
        public async Task Float()
        {
            const float expected = .123456F;
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Real);
            var p2 = new OpenGaussParameter("p2", DbType.Single);
            var p3 = new OpenGaussParameter {ParameterName = "p3", Value = expected};
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            p1.Value = p2.Value = expected;
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFloat(i), Is.EqualTo(expected).Within(10E-07));
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(float)));
            }
        }

        [Test]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public async Task Double_as_float(double value)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p", conn);
            cmd.Parameters.AddWithValue("p", OpenGaussDbType.Real, value);
            var actual = await cmd.ExecuteScalarAsync();
            Assert.That(actual, Is.EqualTo(value));
        }

        [Test, Description("Tests handling of numeric overflow when writing data")]
        [TestCase(OpenGaussDbType.Smallint, 1 + short.MaxValue)]
        [TestCase(OpenGaussDbType.Smallint, 1L + short.MaxValue)]
        [TestCase(OpenGaussDbType.Smallint, 1F + short.MaxValue)]
        [TestCase(OpenGaussDbType.Smallint, 1D + short.MaxValue)]
        [TestCase(OpenGaussDbType.Integer, 1L + int.MaxValue)]
        [TestCase(OpenGaussDbType.Integer, 1F + int.MaxValue)]
        [TestCase(OpenGaussDbType.Integer, 1D + int.MaxValue)]
        [TestCase(OpenGaussDbType.Bigint, 1F + long.MaxValue)]
        [TestCase(OpenGaussDbType.Bigint, 1D + long.MaxValue)]
        [TestCase(OpenGaussDbType.InternalChar, 1 + byte.MaxValue)]
        public async Task Write_overflow(OpenGaussDbType type, object value)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT @p1", conn);

            var p1 = new OpenGaussParameter("p1", type)
            {
                Value = value
            };
            cmd.Parameters.Add(p1);
            Assert.ThrowsAsync<OverflowException>(async () => await cmd.ExecuteScalarAsync());
            Assert.That(await conn.ExecuteScalarAsync("SELECT 1"), Is.EqualTo(1));
        }

        static IEnumerable<TestCaseData> ReadOverflowTestCases
        {
            get
            {
                yield return new TestCaseData(OpenGaussDbType.Smallint, 1D + byte.MaxValue){ };
            }
        }
        [Test, Description("Tests handling of numeric overflow when reading data")]
        [TestCase((byte)0, OpenGaussDbType.Smallint, 1D + byte.MaxValue)]
        [TestCase((sbyte)0, OpenGaussDbType.Smallint, 1D + sbyte.MaxValue)]
        [TestCase((byte)0, OpenGaussDbType.Integer, 1D + byte.MaxValue)]
        [TestCase((short)0, OpenGaussDbType.Integer, 1D + short.MaxValue)]
        [TestCase((byte)0, OpenGaussDbType.Bigint, 1D + byte.MaxValue)]
        [TestCase((short)0, OpenGaussDbType.Bigint, 1D + short.MaxValue)]
        [TestCase(0, OpenGaussDbType.Bigint, 1D + int.MaxValue)]
        public async Task Read_overflow<T>(T readingType, OpenGaussDbType type, double value)
        {
            var typeString = GetTypeAsString(type);
            using (var conn = await OpenConnectionAsync())
            using (var cmd = new OpenGaussCommand($"SELECT {value}::{typeString}", conn))
            {
                Assert.ThrowsAsync<OverflowException>(async() =>
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    Assert.True(reader.Read());
                    reader.GetFieldValue<T>(0);
                });
            }

            string GetTypeAsString(OpenGaussDbType dbType)
                => dbType switch
                {
                    OpenGaussDbType.Smallint => "int2",
                    OpenGaussDbType.Integer  => "int4",
                    OpenGaussDbType.Bigint   => "int8",
                    _                     => throw new NotSupportedException()
                };
        }

        // Older tests

        [Test]
        public async Task Double_without_prepared()
        {
            using var conn = await OpenConnectionAsync();
            using var command = new OpenGaussCommand("select :field_float8", conn);
            command.Parameters.Add(new OpenGaussParameter(":field_float8", OpenGaussDbType.Double));
            var x = 1d/7d;
            command.Parameters[0].Value = x;
            var valueReturned = await command.ExecuteScalarAsync();
            Assert.That(valueReturned, Is.EqualTo(x).Within(100).Ulps);
        }

        [Test]
        public async Task Money()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "select '1'::MONEY, '12345'::MONEY / 100, '123456789012345'::MONEY / 100";
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            Assert.AreEqual(1M, reader.GetValue(0));
            Assert.AreEqual(123.45M, reader.GetValue(1));
            Assert.AreEqual(1234567890123.45M, reader.GetValue(2));
        }

        public NumericTypeTests(MultiplexingMode multiplexingMode) : base(multiplexingMode) {}
    }
}
