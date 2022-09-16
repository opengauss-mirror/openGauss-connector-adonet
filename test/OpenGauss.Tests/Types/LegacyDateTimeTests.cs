using System;
using System.Data;
using System.Threading.Tasks;
using OpenGauss.NET;
using OpenGauss.NET.Types;
using NUnit.Framework;
using static OpenGauss.NET.Util.Statics;
using OpenGauss.NET.TypeMapping;

#pragma warning disable 618 // OpenGaussDateTime is obsolete, remove in 7.0

namespace OpenGauss.Tests.Types
{
    // Since this test suite manipulates TimeZone, it is incompatible with multiplexing
    [NonParallelizable]
    public class LegacyDateTimeTests : TestBase
    {
        static readonly TestCaseData[] TimestampValues =
        {
            new TestCaseData(new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc), "1998-04-12 13:26:38")
                .SetName("TimestampPre2000"),
            new TestCaseData(new DateTime(2015, 1, 27, 8, 45, 12, 345, DateTimeKind.Utc), "2015-01-27 08:45:12.345")
                .SetName("TimestampPost2000"),
            new TestCaseData(new DateTime(2013, 7, 25, 0, 0, 0, DateTimeKind.Utc), "2013-07-25 00:00:00")
                .SetName("TimestampDateOnly"),
        };

        [Test, TestCaseSource(nameof(TimestampValues))]
        public async Task Timestamp_read(DateTime dateTime, string s)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand($"SELECT '{s}'::timestamp without time zone", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(reader.GetDataTypeName(0), Is.EqualTo("timestamp without time zone"));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(DateTime)));

            Assert.That(reader[0], Is.EqualTo(dateTime));
            Assert.That(reader.GetDateTime(0), Is.EqualTo(dateTime));
            Assert.That(reader.GetDateTime(0).Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(reader.GetFieldValue<DateTime>(0), Is.EqualTo(dateTime));

            // Provider-specific type (OpenGaussTimeStamp)
            var opengaussDateTime = new OpenGaussDateTime(dateTime.Ticks);
            Assert.That(reader.GetProviderSpecificFieldType(0), Is.EqualTo(typeof(OpenGaussDateTime)));
            Assert.That(reader.GetTimeStamp(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetFieldValue<OpenGaussDateTime>(0), Is.EqualTo(opengaussDateTime));

            // DateTimeOffset
            Assert.That(() => reader.GetFieldValue<DateTimeOffset>(0), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test, TestCaseSource(nameof(TimestampValues))]
        public async Task Timestamp_write_values(DateTime dateTime, string expected)
        {
            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn)
            {
                Parameters =
                {
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), OpenGaussDbType = OpenGaussDbType.Timestamp }
                }
            };

            Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo(expected));
        }

        static Func<OpenGaussParameter>[] TimestampParameters
        {
            get
            {
                var dateTime = new DateTime(1998, 4, 12, 13, 26, 38);

                return new Func<OpenGaussParameter>[]
                {
                    () => new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified) },
                    () => new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Local) },
                    () => new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) },
                    () => new() { Value = dateTime, OpenGaussDbType = OpenGaussDbType.Timestamp },
                    () => new() { Value = dateTime, DbType = DbType.DateTime },
                    () => new() { Value = dateTime, DbType = DbType.DateTime2 },
                    () => new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Unspecified) },
                    () => new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Local) },
                    () => new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Utc) },
                };
            }
        }

        [Test, TestCaseSource(nameof(TimestampParameters))]
        public async Task Timestamp_resolution(Func<OpenGaussParameter> parameterFunc)
        {
            var parameter = parameterFunc();
            await using var conn = await OpenConnectionAsync();
            conn.TypeMapper.Reset();

            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters = { parameter }
            };

            Assert.That(parameter.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Timestamp));
            Assert.That(parameter.DbType, Is.EqualTo(DbType.DateTime).Or.EqualTo(DbType.DateTime2));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("timestamp without time zone"));
            Assert.That(reader[1], Is.EqualTo("1998-04-12 13:26:38"));
        }

        static OpenGaussParameter[] TimestampInvalidParameters
            => new OpenGaussParameter[]
            {
                new() { Value = new DateTimeOffset(), OpenGaussDbType = OpenGaussDbType.Timestamp }
            };

        [Test, TestCaseSource(nameof(TimestampInvalidParameters))]
        public async Task Timestamp_resolution_failure(OpenGaussParameter parameter)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn)
            {
                Parameters = { parameter }
            };

            Assert.That(() => cmd.ExecuteReaderAsync(), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test, TestCaseSource(nameof(TimestampValues))]
        public async Task Timestamptz_read(DateTime dateTime, string s)
        {
            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand($"SELECT '{s}+00'::timestamp with time zone", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(reader.GetDataTypeName(0), Is.EqualTo("timestamp with time zone"));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(DateTime)));

            Assert.That(reader[0], Is.EqualTo(dateTime.ToLocalTime()));
            Assert.That(reader.GetDateTime(0), Is.EqualTo(dateTime.ToLocalTime()));
            Assert.That(reader.GetFieldValue<DateTime>(0), Is.EqualTo(dateTime.ToLocalTime()));
            Assert.That(reader.GetDateTime(0).Kind, Is.EqualTo(DateTimeKind.Local));

            // DateTimeOffset
            Assert.That(reader.GetFieldValue<DateTimeOffset>(0), Is.EqualTo(new DateTimeOffset(dateTime.ToLocalTime())));

            // Provider-specific type (OpenGaussTimeStamp)
            var opengaussDateTime = new OpenGaussDateTime(dateTime.Ticks);
            Assert.That(reader.GetProviderSpecificFieldType(0), Is.EqualTo(typeof(OpenGaussDateTime)));
            Assert.That(reader.GetTimeStamp(0), Is.EqualTo(opengaussDateTime.ToLocalTime()));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(opengaussDateTime.ToLocalTime()));
            Assert.That(reader.GetFieldValue<OpenGaussDateTime>(0), Is.EqualTo(opengaussDateTime.ToLocalTime()));
            Assert.That(reader.GetTimeStamp(0).Kind, Is.EqualTo(DateTimeKind.Local));
        }

        static readonly TestCaseData[] TimestampTzValues =
        {
            new TestCaseData(new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc), "1998-04-12 15:26:38+02")
                .SetName("TimestampPre2000"),
            new TestCaseData(new DateTime(2015, 1, 27, 8, 45, 12, 345, DateTimeKind.Utc), "2015-01-27 09:45:12.345+01")
                .SetName("TimestampPost2000"),
            new TestCaseData(new DateTime(2013, 7, 25, 0, 0, 0, DateTimeKind.Utc), "2013-07-25 02:00:00+02")
                .SetName("TimestampDateOnly"),
        };

        [Test, TestCaseSource(nameof(TimestampTzValues))]
        public async Task Timestamptz_write_values(DateTime dateTime, string expected)
        {
            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn)
            {
                Parameters = { new() { Value = dateTime, OpenGaussDbType = OpenGaussDbType.TimestampTz } }
            };

            Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo(expected));
        }

        static OpenGaussParameter[] TimestamptzParameters
        {
            get
            {
                var dateTime = new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc);

                return new OpenGaussParameter[]
                {
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = dateTime.ToLocalTime(), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Unspecified), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Utc).ToLocalTime(), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Utc), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new DateTimeOffset(dateTime.ToLocalTime()) }
                };
            }
        }

        [Test, TestCaseSource(nameof(TimestamptzParameters))]
        public async Task Timestamptz_resolution(OpenGaussParameter parameter)
        {
            await using var conn = await OpenConnectionAsync();
            conn.TypeMapper.Reset();

            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters = { parameter }
            };

            Assert.That(parameter.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.TimestampTz));
            Assert.That(parameter.DbType, Is.EqualTo(DbType.DateTimeOffset));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("timestamp with time zone"));
            Assert.That(reader[1], Is.EqualTo("1998-04-12 15:26:38+02"));
        }

        protected override async ValueTask<OpenGaussConnection> OpenConnectionAsync(string? connectionString = null)
        {
            var conn = await base.OpenConnectionAsync(connectionString);
            await conn.ExecuteNonQueryAsync("SET TimeZone='Europe/Berlin'");
            return conn;
        }

        protected override OpenGaussConnection OpenConnection(string? connectionString = null)
            => throw new NotSupportedException();

        [OneTimeSetUp]
        public void Setup()
        {
#if DEBUG
            LegacyTimestampBehavior = true;
            BuiltInTypeHandlerResolver.ResetMappings();
#else
            Assert.Ignore(
                "Legacy DateTime tests rely on the OpenGauss.EnableLegacyTimestampBehavior AppContext switch and can only be run in DEBUG builds");
#endif
        }

        [OneTimeTearDown]
        public void Teardown()
        {
#if DEBUG
            LegacyTimestampBehavior = false;
            BuiltInTypeHandlerResolver.ResetMappings();
#endif
        }
    }
}
