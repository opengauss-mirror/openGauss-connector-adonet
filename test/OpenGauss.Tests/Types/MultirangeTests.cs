using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenGauss.NET.Types;
using NUnit.Framework;
using static OpenGauss.Tests.TestUtil;
using OpenGauss.NET;

namespace OpenGauss.Tests.Types
{
    public class MultirangeTests : TestBase
    {
        [Test]
        public async Task Read()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT '{[3,7), (8,]}'::int4multirange", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(reader.GetDataTypeName(0), Is.EqualTo("int4multirange"));

            var multirangeArray = (OpenGaussRange<int>[])reader[0];
            Assert.That(multirangeArray.Length, Is.EqualTo(2));
            Assert.That(multirangeArray[0], Is.EqualTo(new OpenGaussRange<int>(3, true, false, 7, false, false)));
            Assert.That(multirangeArray[1], Is.EqualTo(new OpenGaussRange<int>(9, true, false, 0, false, true)));

            var multirangeList = reader.GetFieldValue<List<OpenGaussRange<int>>>(0);
            Assert.That(multirangeList.Count, Is.EqualTo(2));
            Assert.That(multirangeList[0], Is.EqualTo(new OpenGaussRange<int>(3, true, false, 7, false, false)));
            Assert.That(multirangeList[1], Is.EqualTo(new OpenGaussRange<int>(9, true, false, 0, false, true)));
        }

        [Test]
        public async Task Write()
        {
            var multirangeArray = new OpenGaussRange<int>[]
            {
                new(3, true, false, 7, false, false),
                new(8, false, false, 0, false, true)
            };

            var multirangeList = new List<OpenGaussRange<int>>(multirangeArray);

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn);

            await WriteInternal(multirangeArray);
            await WriteInternal(multirangeList);

            async Task WriteInternal(IList<OpenGaussRange<int>> multirange)
            {
                conn.ReloadTypes();
                cmd.Parameters.Add(new() { Value = multirange });
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),[9,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, OpenGaussDbType = OpenGaussDbType.IntegerMultirange };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),[9,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, DataTypeName = "int4multirange" };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),[9,)}"));
            }
        }

        [Test]
        public async Task Write_nummultirange()
        {
            var multirangeArray = new OpenGaussRange<decimal>[]
            {
                new(3, true, false, 7, false, false),
                new(8, false, false, 0, false, true)
            };

            var multirangeList = new List<OpenGaussRange<decimal>>(multirangeArray);

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn);

            await WriteInternal(multirangeArray);
            await WriteInternal(multirangeList);

            async Task WriteInternal(IList<OpenGaussRange<decimal>> multirange)
            {
                conn.ReloadTypes();
                cmd.Parameters.Add(new() { Value = multirange });
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),(8,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, OpenGaussDbType = OpenGaussDbType.NumericMultirange };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),(8,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, DataTypeName = "nummultirange" };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[3,7),(8,)}"));
            }
        }

        [Test]
        public async Task Read_Datemultirange()
        {
            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT '{[2020-01-01,2020-01-05), (2020-01-10,]}'::datemultirange", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.That(reader.GetDataTypeName(0), Is.EqualTo("datemultirange"));

            var multirangeDateTimeArray = (OpenGaussRange<DateTime>[])reader[0];
            Assert.That(multirangeDateTimeArray.Length, Is.EqualTo(2));
            Assert.That(multirangeDateTimeArray[0], Is.EqualTo(new OpenGaussRange<DateTime>(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false)));
            Assert.That(multirangeDateTimeArray[1], Is.EqualTo(new OpenGaussRange<DateTime>(new(2020, 1, 11), true, false, default, false, true)));

            var multirangeDateTimeList = reader.GetFieldValue<List<OpenGaussRange<DateTime>>>(0);
            Assert.That(multirangeDateTimeList.Count, Is.EqualTo(2));
            Assert.That(multirangeDateTimeList[0], Is.EqualTo(new OpenGaussRange<DateTime>(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false)));
            Assert.That(multirangeDateTimeList[1], Is.EqualTo(new OpenGaussRange<DateTime>(new(2020, 1, 11), true, false, default, false, true)));

#if NET6_0_OR_GREATER
            var multirangeDateOnlyArray = reader.GetFieldValue<OpenGaussRange<DateOnly>[]>(0);
            Assert.That(multirangeDateOnlyArray.Length, Is.EqualTo(2));
            Assert.That(multirangeDateOnlyArray[0], Is.EqualTo(new OpenGaussRange<DateOnly>(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false)));
            Assert.That(multirangeDateOnlyArray[1], Is.EqualTo(new OpenGaussRange<DateOnly>(new(2020, 1, 11), true, false, default, false, true)));

            var multirangeDateOnlyList = reader.GetFieldValue<List<OpenGaussRange<DateOnly>>>(0);
            Assert.That(multirangeDateOnlyList.Count, Is.EqualTo(2));
            Assert.That(multirangeDateOnlyList[0], Is.EqualTo(new OpenGaussRange<DateOnly>(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false)));
            Assert.That(multirangeDateOnlyList[1], Is.EqualTo(new OpenGaussRange<DateOnly>(new(2020, 1, 11), true, false, default, false, true)));
#endif
        }

        [Test]
        public async Task Write_Datemultirange_DateOnly()
        {
            var multirangeArray = new OpenGaussRange<DateOnly>[]
            {
                new(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false),
                new(new(2020, 1, 10), false, false, default, false, true)
            };

            var multirangeList = new List<OpenGaussRange<DateOnly>>(multirangeArray);

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn);

            await WriteInternal(multirangeArray);
            await WriteInternal(multirangeList);

            async Task WriteInternal(IList<OpenGaussRange<DateOnly>> multirange)
            {
                conn.ReloadTypes();
                cmd.Parameters.Add(new() { Value = multirange });
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[2020-01-01,2020-01-05),[2020-01-11,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, OpenGaussDbType = OpenGaussDbType.DateMultirange };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[2020-01-01,2020-01-05),[2020-01-11,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, DataTypeName = "datemultirange" };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[2020-01-01,2020-01-05),[2020-01-11,)}"));
            }
        }

        [Test]
        public async Task Write_Datemultirange_DateTime()
        {
            var multirangeArray = new OpenGaussRange<DateTime>[]
            {
                new(new(2020, 1, 1), true, false, new(2020, 1, 5), false, false),
                new(new(2020, 1, 10), false, false, default, false, true)
            };

            var multirangeList = new List<OpenGaussRange<DateTime>>(multirangeArray);

            await using var conn = await OpenConnectionAsync();
            await using var cmd = new OpenGaussCommand("SELECT $1::text", conn);

            await WriteInternal(multirangeArray);
            await WriteInternal(multirangeList);

            async Task WriteInternal(IList<OpenGaussRange<DateTime>> multirange)
            {
                conn.ReloadTypes();
                cmd.Parameters.Add(new() { Value = multirange, OpenGaussDbType = OpenGaussDbType.DateMultirange });
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[2020-01-01,2020-01-05),[2020-01-11,)}"));

                conn.ReloadTypes();
                cmd.Parameters[0] = new() { Value = multirange, DataTypeName = "datemultirange" };
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo("{[2020-01-01,2020-01-05),[2020-01-11,)}"));
            }
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            await using var conn = await OpenConnectionAsync();
            MinimumPgVersion(conn, "14.0", "Multirange types were introduced in PostgreSQL 14");
        }
    }
}
