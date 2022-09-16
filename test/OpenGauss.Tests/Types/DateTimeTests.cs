using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using OpenGauss.NET.Types;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using static OpenGauss.Tests.TestUtil;
using OpenGauss.NET;

#pragma warning disable 618 // OpenGaussDateTime, OpenGaussDate and OpenGaussTimespan are obsolete, remove in 7.0

namespace OpenGauss.Tests.Types
{
    // Since this test suite manipulates TimeZone, it is incompatible with multiplexing
    public class DateTimeTests : TestBase
    {
        #region Date

        [Test]
        public async Task Date()
        {
            using var conn = await OpenConnectionAsync();
            var dateTime = new DateTime(2002, 3, 4, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var opengaussDate = new OpenGaussDate(dateTime);

            using var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Date) {Value = opengaussDate};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = opengaussDate};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Date));
            Assert.That(p2.DbType, Is.EqualTo(DbType.Date));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                // Regular type (DateTime)
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(DateTime)));
                Assert.That(reader.GetDateTime(i), Is.EqualTo(dateTime));
                Assert.That(reader.GetFieldValue<DateTime>(i), Is.EqualTo(dateTime));
                Assert.That(reader[i], Is.EqualTo(dateTime));
                Assert.That(reader.GetValue(i), Is.EqualTo(dateTime));

                // Provider-specific type (OpenGaussDate)
                Assert.That(reader.GetDate(i), Is.EqualTo(opengaussDate));
                Assert.That(reader.GetProviderSpecificFieldType(i), Is.EqualTo(typeof(OpenGaussDate)));
                Assert.That(reader.GetProviderSpecificValue(i), Is.EqualTo(opengaussDate));
                Assert.That(reader.GetFieldValue<OpenGaussDate>(i), Is.EqualTo(opengaussDate));

                // Internal PostgreSQL representation, for out-of-range values.
                Assert.That(() => reader.GetInt32(0), Throws.Nothing);
            }
        }

#if NET6_0_OR_GREATER
        [Test]
        public async Task Date_DateOnly()
        {
            using var conn = await OpenConnectionAsync();
            var dateOnly = new DateOnly(2002, 3, 4);
            var dateTime = dateOnly.ToDateTime(default);

            using var cmd = new OpenGaussCommand("SELECT @p1", conn);
            var p1 = new OpenGaussParameter { ParameterName = "p1", Value = dateOnly };
            Assert.That(p1.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Date));
            Assert.That(p1.DbType, Is.EqualTo(DbType.Date));
            cmd.Parameters.Add(p1);

            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            Assert.That(reader.GetFieldValue<DateOnly>(0), Is.EqualTo(dateOnly));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(DateTime)));
            Assert.That(reader.GetDateTime(0), Is.EqualTo(dateTime));
            Assert.That(reader[0], Is.EqualTo(dateTime));
            Assert.That(reader.GetValue(0), Is.EqualTo(dateTime));
        }

        [Test]
        public async Task Date_DateOnly_range()
        {
            using var conn = await OpenConnectionAsync();
            var range = new OpenGaussRange<DateOnly>(new(2002, 3, 4), true, new(2002, 3, 6), false);

            using var cmd = new OpenGaussCommand("SELECT @p1", conn);
            var p1 = new OpenGaussParameter { ParameterName = "p1", Value = range };
            Assert.That(p1.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.DateRange));
            Assert.That(p1.DbType, Is.EqualTo(DbType.Object));
            cmd.Parameters.Add(p1);

            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            Assert.That(reader.GetFieldValue<OpenGaussRange<DateOnly>>(0), Is.EqualTo(range));
        }
#endif

        #endregion

        #region Time

        [Test]
        public async Task Time()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new TimeSpan(0, 10, 45, 34, 500);

            using var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            cmd.Parameters.Add(new OpenGaussParameter("p1", OpenGaussDbType.Time) {Value = expected});
            cmd.Parameters.Add(new OpenGaussParameter("p2", DbType.Time) {Value = expected});
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(TimeSpan)));
                Assert.That(reader.GetTimeSpan(i), Is.EqualTo(expected));
                Assert.That(reader.GetFieldValue<TimeSpan>(i), Is.EqualTo(expected));
                Assert.That(reader[i], Is.EqualTo(expected));
                Assert.That(reader.GetValue(i), Is.EqualTo(expected));
            }
        }

#if NET6_0_OR_GREATER
        [Test]
        public async Task Time_TimeOnly()
        {
            using var conn = await OpenConnectionAsync();
            var timeOnly = new TimeOnly(10, 45, 34, 500);
            var timeSpan = timeOnly.ToTimeSpan();

            using var cmd = new OpenGaussCommand("SELECT @p1", conn);
            cmd.Parameters.Add(new OpenGaussParameter { ParameterName = "p1", Value = timeOnly });

            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            Assert.That(reader.GetFieldValue<TimeOnly>(0), Is.EqualTo(timeOnly));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(TimeSpan)));
            Assert.That(reader.GetTimeSpan(0), Is.EqualTo(timeSpan));
            Assert.That(reader.GetFieldValue<TimeSpan>(0), Is.EqualTo(timeSpan));
            Assert.That(reader[0], Is.EqualTo(timeSpan));
            Assert.That(reader.GetValue(0), Is.EqualTo(timeSpan));
        }
#endif

        #endregion

        #region Time with timezone

        [Test]
        [MonoIgnore]
        public async Task TimeTz()
        {
            using var conn = await OpenConnectionAsync();
            var tzOffset = TimeZoneInfo.Local.BaseUtcOffset;
            if (tzOffset == TimeSpan.Zero)
                Assert.Ignore("Test cannot run when machine timezone is UTC");

            // Note that the date component of the below is ignored
            var dto = new DateTimeOffset(5, 5, 5, 13, 3, 45, 510, tzOffset);

            using var cmd = new OpenGaussCommand("SELECT @p", conn);
            cmd.Parameters.AddWithValue("p", OpenGaussDbType.TimeTz, dto);
            Assert.That(cmd.Parameters.All(p => p.DbType == DbType.Object));

            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(DateTimeOffset)));
                Assert.That(reader.GetFieldValue<DateTimeOffset>(i), Is.EqualTo(new DateTimeOffset(1, 1, 2, dto.Hour, dto.Minute, dto.Second, dto.Millisecond, dto.Offset)));
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(DateTimeOffset)));
            }
        }

        [Test]
        public async Task TimeTz_before_utc_zero()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT TIME WITH TIME ZONE '01:00:00+02'", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();
            Assert.That(reader.GetFieldValue<DateTimeOffset>(0), Is.EqualTo(new DateTimeOffset(1, 1, 2, 1, 0, 0, new TimeSpan(0, 2, 0, 0))));
        }

        #endregion

        #region Timestamp

        static readonly TestCaseData[] TimestampValues =
        {
            new TestCaseData(new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Unspecified), "1998-04-12 13:26:38")
                .SetName("TimestampPre2000"),
            new TestCaseData(new DateTime(2015, 1, 27, 8, 45, 12, 345, DateTimeKind.Unspecified), "2015-01-27 08:45:12.345")
                .SetName("TimestampPost2000"),
            new TestCaseData(new DateTime(2013, 7, 25, 0, 0, 0, DateTimeKind.Unspecified), "2013-07-25 00:00:00")
                .SetName("TimestampDateOnly")
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
            var opengaussDateTime = new OpenGaussDateTime(dateTime);
            Assert.That(reader.GetProviderSpecificFieldType(0), Is.EqualTo(typeof(OpenGaussDateTime)));
            Assert.That(reader.GetTimeStamp(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetFieldValue<OpenGaussDateTime>(0), Is.EqualTo(opengaussDateTime));

            // DateTimeOffset
            Assert.That(() => reader.GetFieldValue<DateTimeOffset>(0), Throws.Exception.TypeOf<InvalidCastException>());

            // Internal PostgreSQL representation, for out-of-range values.
            Assert.That(() => reader.GetInt64(0), Throws.Nothing);
        }

        [Test, TestCaseSource(nameof(TimestampValues))]
        public async Task Timestamp_write_values(DateTime dateTime, string expected)
        {
            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Unspecified));

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

        static OpenGaussParameter[] TimestampParameters
        {
            get
            {
                var dateTime = new DateTime(1998, 4, 12, 13, 26, 38);

                return new OpenGaussParameter[]
                {
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified) },
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Local) },
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Local), OpenGaussDbType = OpenGaussDbType.Timestamp },
                    new() { Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Local), DbType = DbType.DateTime2 },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Unspecified) },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Local) },
                    new() { Value = -54297202000000L, OpenGaussDbType = OpenGaussDbType.Timestamp }
                };
            }
        }

        [Test, TestCaseSource(nameof(TimestampParameters))]
        public async Task Timestamp_resolution(OpenGaussParameter parameter)
        {
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
                new() { Value = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), OpenGaussDbType = OpenGaussDbType.Timestamp },
                new() { Value = new OpenGaussDateTime(0, DateTimeKind.Utc), OpenGaussDbType = OpenGaussDbType.Timestamp },
                new() { Value = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero), OpenGaussDbType = OpenGaussDbType.Timestamp }
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

        [Test]
        public async Task Timestamp_array_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters = { new() { Value = new[] { new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Local) } } }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("timestamp without time zone[]"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Array | OpenGaussDbType.Timestamp));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("timestamp without time zone[]"));
            Assert.That(reader[1], Is.EqualTo(@"{""1998-04-12 13:26:38""}"));
        }

        [Test]
        public async Task Timestamp_range_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new OpenGaussRange<DateTime>(
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Local),
                            new DateTime(1998, 4, 12, 15, 26, 38, DateTimeKind.Local))
                    }
                }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("tsrange"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Range | OpenGaussDbType.Timestamp));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("tsrange"));
            Assert.That(reader[1], Is.EqualTo(@"[""1998-04-12 13:26:38"",""1998-04-12 15:26:38""]"));
        }

        [Test]
        public async Task Timestamp_multirange_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            MinimumPgVersion(conn, "14.0", "Multirange types were introduced in PostgreSQL 14");
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new[]
                        {
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Local),
                                new DateTime(1998, 4, 12, 15, 26, 38, DateTimeKind.Local)),
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 13, 13, 26, 38, DateTimeKind.Local),
                                new DateTime(1998, 4, 13, 15, 26, 38, DateTimeKind.Local)),
                        }
                    }
                }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("tsmultirange"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Multirange | OpenGaussDbType.Timestamp));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("tsmultirange"));
            Assert.That(reader[1], Is.EqualTo(@"{[""1998-04-12 13:26:38"",""1998-04-12 15:26:38""],[""1998-04-13 13:26:38"",""1998-04-13 15:26:38""]}"));
        }

        #endregion

        #region Timestamp with timezone

        static readonly TestCaseData[] TimestampTzReadValues =
        {
            new TestCaseData(new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc), "1998-04-12 13:26:38+00")
                .SetName("TimestampPre2000"),
            new TestCaseData(new DateTime(2015, 1, 27, 8, 45, 12, 345, DateTimeKind.Utc), "2015-01-27 08:45:12.345+00")
                .SetName("TimestampPost2000"),
            new TestCaseData(new DateTime(2013, 7, 25, 0, 0, 0, DateTimeKind.Utc), "2013-07-25 00:00:00+00")
                .SetName("TimestampDateOnly")
        };

        [Test, TestCaseSource(nameof(TimestampTzReadValues))]
        public async Task Timestamptz_read(DateTime dateTime, string s)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand($"SELECT '{s}'::timestamp with time zone", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(reader.GetDataTypeName(0), Is.EqualTo("timestamp with time zone"));
            Assert.That(reader.GetFieldType(0), Is.EqualTo(typeof(DateTime)));

            Assert.That(reader[0], Is.EqualTo(dateTime));
            Assert.That(reader.GetDateTime(0), Is.EqualTo(dateTime));
            Assert.That(reader.GetFieldValue<DateTime>(0), Is.EqualTo(dateTime));
            Assert.That(reader.GetDateTime(0).Kind, Is.EqualTo(DateTimeKind.Utc));

            // DateTimeOffset
            Assert.That(reader.GetFieldValue<DateTimeOffset>(0), Is.EqualTo(new DateTimeOffset(dateTime, TimeSpan.Zero)));
            Assert.That(reader.GetFieldValue<DateTimeOffset>(0).Offset, Is.EqualTo(TimeSpan.Zero));

            // Provider-specific type (OpenGaussTimeStamp)
            var opengaussDateTime = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Utc);
            Assert.That(reader.GetProviderSpecificFieldType(0), Is.EqualTo(typeof(OpenGaussDateTime)));
            Assert.That(reader.GetTimeStamp(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetProviderSpecificValue(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetFieldValue<OpenGaussDateTime>(0), Is.EqualTo(opengaussDateTime));
            Assert.That(reader.GetTimeStamp(0).Kind, Is.EqualTo(DateTimeKind.Utc));

            // Internal PostgreSQL representation, for out-of-range values.
            Assert.That(() => reader.GetInt64(0), Throws.Nothing);
        }

        static readonly TestCaseData[] TimestampTzWriteValues =
        {
            new TestCaseData(new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc), "1998-04-12 13:26:38")
                .SetName("TimestampTzPre2000"),
            new TestCaseData(new DateTime(2015, 1, 27, 8, 45, 12, 345, DateTimeKind.Utc), "2015-01-27 08:45:12.345")
                .SetName("TimestampTzPost2000"),
            new TestCaseData(new DateTime(2013, 7, 25, 0, 0, 0, DateTimeKind.Utc), "2013-07-25 00:00:00")
                .SetName("TimestampTzDateOnly"),
            new TestCaseData(OpenGaussDateTime.Infinity, "infinity")
                .SetName("TimestampTzOpenGaussDateTimeInfinity"),
            new TestCaseData(OpenGaussDateTime.NegativeInfinity, "-infinity")
                .SetName("TimestampTzOpenGaussDateTimeNegativeInfinity"),
            new TestCaseData(new OpenGaussDateTime(-5, 3, 3, 1, 0, 0, DateTimeKind.Utc), "0005-03-03 01:00:00 BC")
                .SetName("TimestampTzBC"),
            new TestCaseData(DateTime.MinValue, "-infinity")
                .SetName("TimestampNegativeInfinity"),
            new TestCaseData(DateTime.MaxValue, "infinity")
                .SetName("TimestampInfinity")
        };

        [Test, TestCaseSource(nameof(TimestampTzWriteValues))]
        public async Task Timestamptz_write_values(object dateTime, string expected)
        {
            await using var conn = await OpenConnectionAsync();

            // PG sends local timestamptz *text* representations (according to TimeZone). Convert to a timestamp without time zone at UTC
            // for sensible assertions.
            await using var cmd = new OpenGaussCommand("SELECT ($1 AT TIME ZONE 'UTC')::text", conn)
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
                    new() { Value = dateTime },
                    new() { Value = dateTime, OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new OpenGaussDateTime(dateTime.Ticks, DateTimeKind.Utc), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                    new() { Value = new DateTimeOffset(dateTime) },
                    new() { Value = -54297202000000L, OpenGaussDbType = OpenGaussDbType.TimestampTz }
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

            Assert.That(parameter.DataTypeName, Is.EqualTo("timestamp with time zone"));
            Assert.That(parameter.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.TimestampTz));
            Assert.That(parameter.DbType, Is.EqualTo(DbType.DateTime).Or.EqualTo(DbType.DateTimeOffset));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("timestamp with time zone"));
            Assert.That(reader[1], Is.EqualTo("1998-04-12 15:26:38+02"));
        }

        static OpenGaussParameter[] TimestamptzInvalidParameters
            => new OpenGaussParameter[]
            {
                new() { Value = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                new() { Value = DateTime.Now, OpenGaussDbType = OpenGaussDbType.TimestampTz },
                new() { Value = new OpenGaussDateTime(0, DateTimeKind.Unspecified), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                new() { Value = new OpenGaussDateTime(0, DateTimeKind.Local), OpenGaussDbType = OpenGaussDbType.TimestampTz },
                new() { Value = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), TimeSpan.FromHours(2)) }
            };

        [Test, TestCaseSource(nameof(TimestamptzInvalidParameters))]
        public async Task Timestamptz_resolution_failure(OpenGaussParameter parameter)
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn)
            {
                Parameters = { parameter }
            };

            Assert.That(() => cmd.ExecuteReaderAsync(), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test]
        public async Task Timestamptz_array_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters = { new() { Value = new[] { new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc) } } }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("timestamp with time zone[]"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Array | OpenGaussDbType.TimestampTz));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("timestamp with time zone[]"));
            Assert.That(reader[1], Is.EqualTo(@"{""1998-04-12 15:26:38+02""}"));
        }

        [Test]
        public async Task Timestamptz_range_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new OpenGaussRange<DateTime>(
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc),
                            new DateTime(1998, 4, 12, 15, 26, 38, DateTimeKind.Utc))
                    }
                }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("tstzrange"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Range | OpenGaussDbType.TimestampTz));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("tstzrange"));
            Assert.That(reader[1], Is.EqualTo(@"[""1998-04-12 15:26:38+02"",""1998-04-12 17:26:38+02""]"));
        }

        [Test]
        public async Task Timestamptz_multirange_resolution()
        {
            await using var conn = await OpenConnectionAsync();
            MinimumPgVersion(conn, "14.0", "Multirange types were introduced in PostgreSQL 14");
            await using var cmd = new OpenGaussCommand("SELECT pg_typeof($1)::text, $1::text", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new[]
                        {
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc),
                                new DateTime(1998, 4, 12, 15, 26, 38, DateTimeKind.Utc)),
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 13, 13, 26, 38, DateTimeKind.Utc),
                                new DateTime(1998, 4, 13, 15, 26, 38, DateTimeKind.Utc)),
                        }
                    }
                }
            };

            Assert.That(cmd.Parameters[0].DataTypeName, Is.EqualTo("tstzmultirange"));
            Assert.That(cmd.Parameters[0].OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Multirange | OpenGaussDbType.TimestampTz));
            Assert.That(cmd.Parameters[0].DbType, Is.EqualTo(DbType.Object));

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            Assert.That(reader[0], Is.EqualTo("tstzmultirange"));
            Assert.That(reader[1], Is.EqualTo(@"{[""1998-04-12 15:26:38+02"",""1998-04-12 17:26:38+02""],[""1998-04-13 15:26:38+02"",""1998-04-13 17:26:38+02""]}"));
        }

        [Test]
        public async Task Cannot_mix_DateTime_Kinds_in_array()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new[]
                        {
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc),
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Local),
                        }
                    }
                }
            };

            Assert.That(() => cmd.ExecuteReaderAsync(), Throws.Exception.TypeOf<Exception>());
        }

        [Test]
        public async Task Cannot_mix_DateTime_Kinds_in_range()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new OpenGaussRange<DateTime>(
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc),
                            new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Local))
                    }
                }
            };

            Assert.That(() => cmd.ExecuteReaderAsync(), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test]
        public async Task Cannot_mix_DateTime_Kinds_in_multirange()
        {
            await using var conn = await OpenConnectionAsync();
            MinimumPgVersion(conn, "14.0", "Multirange types were introduced in PostgreSQL 14");
            await using var cmd = new OpenGaussCommand("SELECT $1", conn)
            {
                Parameters =
                {
                    new()
                    {
                        Value = new[]
                        {
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 12, 13, 26, 38, DateTimeKind.Utc),
                                new DateTime(1998, 4, 12, 15, 26, 38, DateTimeKind.Utc)),
                            new OpenGaussRange<DateTime>(
                                new DateTime(1998, 4, 13, 13, 26, 38, DateTimeKind.Local),
                                new DateTime(1998, 4, 13, 15, 26, 38, DateTimeKind.Local)),
                        }
                    }
                }
            };

            Assert.That(() => cmd.ExecuteReaderAsync(), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test]
        public void OpenGaussParameterDbType_is_value_dependent_datetime_or_datetime2()
        {
            var localtimestamp = new OpenGaussParameter { Value = DateTime.Now };
            var unspecifiedtimestamp = new OpenGaussParameter { Value = new DateTime() };
            Assert.AreEqual(DbType.DateTime2, localtimestamp.DbType);
            Assert.AreEqual(DbType.DateTime2, unspecifiedtimestamp.DbType);

            // We don't support any DateTimeOffset other than offset 0 which maps to timestamptz,
            // we might add an exception for offset == DateTimeOffset.Now.Offset (local offset) mapping to timestamp at some point.
            // var dtotimestamp = new OpenGaussParameter { Value = DateTimeOffset.Now };
            // Assert.AreEqual(DbType.DateTime2, dtotimestamp.DbType);

            var timestamptz = new OpenGaussParameter { Value = DateTime.UtcNow };
            var dtotimestamptz = new OpenGaussParameter { Value = DateTimeOffset.UtcNow };
            Assert.AreEqual(DbType.DateTime, timestamptz.DbType);
            Assert.AreEqual(DbType.DateTime, dtotimestamptz.DbType);
        }

        [Test]
        public void OpenGaussParameterOpenGaussDbType_is_value_dependent_timestamp_or_timestamptz()
        {
            var localtimestamp = new OpenGaussParameter { Value = DateTime.Now };
            var unspecifiedtimestamp = new OpenGaussParameter { Value = new DateTime() };
            Assert.AreEqual(OpenGaussDbType.Timestamp, localtimestamp.OpenGaussDbType);
            Assert.AreEqual(OpenGaussDbType.Timestamp, unspecifiedtimestamp.OpenGaussDbType);

            var timestamptz = new OpenGaussParameter { Value = DateTime.UtcNow };
            var dtotimestamptz = new OpenGaussParameter { Value = DateTimeOffset.UtcNow };
            Assert.AreEqual(OpenGaussDbType.TimestampTz, timestamptz.OpenGaussDbType);
            Assert.AreEqual(OpenGaussDbType.TimestampTz, dtotimestamptz.OpenGaussDbType);
        }

        #endregion

        #region Interval

        [Test]
        public async Task Interval()
        {
            using var conn = await OpenConnectionAsync();
            var expectedOpenGaussTimeSpan = new OpenGaussTimeSpan(1, 2, 3, 4, 5);
            var expectedTimeSpan = new TimeSpan(1, 2, 3, 4, 5);
            var expectedOpenGaussInterval = new OpenGaussInterval(0, 1, 7384005000);

            using var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3, @p4", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Interval);
            var p2 = new OpenGaussParameter("p2", expectedTimeSpan);
            var p3 = new OpenGaussParameter("p3", expectedOpenGaussTimeSpan);
            var p4 = new OpenGaussParameter("p4", expectedOpenGaussInterval);
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Interval));
            Assert.That(p2.DbType, Is.EqualTo(DbType.Object));
            Assert.That(p3.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Interval));
            Assert.That(p3.DbType, Is.EqualTo(DbType.Object));
            Assert.That(p4.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Interval));
            Assert.That(p4.DbType, Is.EqualTo(DbType.Object));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            cmd.Parameters.Add(p4);
            p1.Value = expectedOpenGaussTimeSpan;

            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                // Regular type (TimeSpan)
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(TimeSpan)));
                Assert.That(reader.GetTimeSpan(i), Is.EqualTo(expectedTimeSpan));
                Assert.That(reader.GetFieldValue<TimeSpan>(i), Is.EqualTo(expectedTimeSpan));
                Assert.That(reader[i], Is.EqualTo(expectedTimeSpan));
                Assert.That(reader.GetValue(i), Is.EqualTo(expectedTimeSpan));

                // Provider-specific type (OpenGaussInterval)
                Assert.That(reader.GetInterval(i), Is.EqualTo(expectedOpenGaussTimeSpan));
                Assert.That(reader.GetProviderSpecificFieldType(i), Is.EqualTo(typeof(OpenGaussTimeSpan)));
                Assert.That(reader.GetProviderSpecificValue(i), Is.EqualTo(expectedOpenGaussTimeSpan));
                Assert.That(reader.GetFieldValue<OpenGaussTimeSpan>(i), Is.EqualTo(expectedOpenGaussTimeSpan));

                // Internal PostgreSQL representation, for out-of-range values.
                Assert.That(() => reader.GetFieldValue<OpenGaussInterval>(i), Is.EqualTo(expectedOpenGaussInterval));
            }
        }

        [Test]
        public async Task Interval_with_months_cannot_read_as_TimeSpan()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand("SELECT '1 month 2 days'::interval", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(() => reader.GetTimeSpan(0), Throws.Exception.TypeOf<InvalidCastException>());
        }

        #endregion

        protected override async ValueTask<OpenGaussConnection> OpenConnectionAsync(string? connectionString = null)
        {
            var conn = await base.OpenConnectionAsync(connectionString);
            await conn.ExecuteNonQueryAsync("SET TimeZone='Europe/Berlin'");
            return conn;
        }

        protected override OpenGaussConnection OpenConnection(string? connectionString = null)
            => throw new NotSupportedException();
    }
}
