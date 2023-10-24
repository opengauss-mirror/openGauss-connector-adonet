using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandlers;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NodaTime.NET.Internal
{
    public partial class TimestampTzMultirangeHandler : MultirangeHandler<Instant>,
        IOpenGaussTypeHandler<Interval[]>, IOpenGaussTypeHandler<List<Interval>>,
        IOpenGaussTypeHandler<OpenGaussRange<ZonedDateTime>[]>, IOpenGaussTypeHandler<List<OpenGaussRange<ZonedDateTime>>>,
        IOpenGaussTypeHandler<OpenGaussRange<OffsetDateTime>[]>, IOpenGaussTypeHandler<List<OpenGaussRange<OffsetDateTime>>>,
        IOpenGaussTypeHandler<OpenGaussRange<DateTime>[]>, IOpenGaussTypeHandler<List<OpenGaussRange<DateTime>>>,
        IOpenGaussTypeHandler<OpenGaussRange<DateTimeOffset>[]>, IOpenGaussTypeHandler<List<OpenGaussRange<DateTimeOffset>>>
    {
        readonly IOpenGaussTypeHandler<Interval> _intervalHandler;

        public override Type GetFieldType(FieldDescription? fieldDescription = null) => typeof(Interval[]);
        public override Type GetProviderSpecificFieldType(FieldDescription? fieldDescription = null) => typeof(Interval[]);

        public TimestampTzMultirangeHandler(PostgresMultirangeType pgMultirangeType, TimestampTzRangeHandler rangeHandler)
            : base(pgMultirangeType, rangeHandler)
            => _intervalHandler = rangeHandler;

        public override async ValueTask<object> ReadAsObject(OpenGaussReadBuffer buf, int len, bool async,
            FieldDescription? fieldDescription = null)
            => (await Read<Interval[]>(buf, len, async, fieldDescription))!;

        async ValueTask<Interval[]> IOpenGaussTypeHandler<Interval[]>.Read(OpenGaussReadBuffer buf, int len, bool async,
            FieldDescription? fieldDescription)
        {
            await buf.Ensure(4, async);
            var numRanges = buf.ReadInt32();
            var multirange = new Interval[numRanges];

            for (var i = 0; i < multirange.Length; i++)
            {
                await buf.Ensure(4, async);
                var rangeLen = buf.ReadInt32();
                multirange[i] = await _intervalHandler.Read(buf, rangeLen, async, fieldDescription);
            }

            return multirange;
        }

        async ValueTask<List<Interval>> IOpenGaussTypeHandler<List<Interval>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            await buf.Ensure(4, async);
            var numRanges = buf.ReadInt32();
            var multirange = new List<Interval>(numRanges);

            for (var i = 0; i < numRanges; i++)
            {
                await buf.Ensure(4, async);
                var rangeLen = buf.ReadInt32();
                multirange.Add(await _intervalHandler.Read(buf, rangeLen, async, fieldDescription));
            }

            return multirange;
        }

        public int ValidateAndGetLength(List<Interval> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthCore(value, ref lengthCache);

        public int ValidateAndGetLength(Interval[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthCore(value, ref lengthCache);

        int ValidateAndGetLengthCore(IList<Interval> value, ref OpenGaussLengthCache? lengthCache)
        {
            lengthCache ??= new OpenGaussLengthCache(1);
            if (lengthCache.IsPopulated)
                return lengthCache.Get();

            var sum = 4 + 4 * value.Count;
            for (var i = 0; i < value.Count; i++)
                sum += _intervalHandler.ValidateAndGetLength(value[i], ref lengthCache, parameter: null);

            return lengthCache!.Set(sum);
        }

        public async Task Write(Interval[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 4)
                await buf.Flush(async, cancellationToken);

            buf.WriteInt32(value.Length);

            for (var i = 0; i < value.Length; i++)
                await RangeHandler.WriteWithLength(value[i], buf, lengthCache, parameter: null, async, cancellationToken);
        }

        public async Task Write(List<Interval> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 4)
                await buf.Flush(async, cancellationToken);

            buf.WriteInt32(value.Count);

            for (var i = 0; i < value.Count; i++)
                await RangeHandler.WriteWithLength(value[i], buf, lengthCache, parameter: null, async, cancellationToken);
        }

        #region Boilerplate

        ValueTask<OpenGaussRange<ZonedDateTime>[]> IOpenGaussTypeHandler<OpenGaussRange<ZonedDateTime>[]>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeArray<ZonedDateTime>(buf, len, async, fieldDescription);

        ValueTask<List<OpenGaussRange<ZonedDateTime>>> IOpenGaussTypeHandler<List<OpenGaussRange<ZonedDateTime>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<ZonedDateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<OffsetDateTime>[]> IOpenGaussTypeHandler<OpenGaussRange<OffsetDateTime>[]>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeArray<OffsetDateTime>(buf, len, async, fieldDescription);

        ValueTask<List<OpenGaussRange<OffsetDateTime>>> IOpenGaussTypeHandler<List<OpenGaussRange<OffsetDateTime>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<OffsetDateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<DateTime>[]> IOpenGaussTypeHandler<OpenGaussRange<DateTime>[]>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeArray<DateTime>(buf, len, async, fieldDescription);

        ValueTask<List<OpenGaussRange<DateTime>>> IOpenGaussTypeHandler<List<OpenGaussRange<DateTime>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<DateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<DateTimeOffset>[]> IOpenGaussTypeHandler<OpenGaussRange<DateTimeOffset>[]>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeArray<DateTimeOffset>(buf, len, async, fieldDescription);

        ValueTask<List<OpenGaussRange<DateTimeOffset>>> IOpenGaussTypeHandler<List<OpenGaussRange<DateTimeOffset>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<DateTimeOffset>(buf, len, async, fieldDescription);

        public int ValidateAndGetLength(OpenGaussRange<ZonedDateTime>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(List<OpenGaussRange<ZonedDateTime>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<OffsetDateTime>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(List<OpenGaussRange<OffsetDateTime>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<DateTime>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(List<OpenGaussRange<DateTime>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<DateTimeOffset>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(List<OpenGaussRange<DateTimeOffset>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public Task Write(OpenGaussRange<ZonedDateTime>[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
                OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(List<OpenGaussRange<ZonedDateTime>> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<OffsetDateTime>[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(List<OpenGaussRange<OffsetDateTime>> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<DateTime>[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(List<OpenGaussRange<DateTime>> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<DateTimeOffset>[] value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(List<OpenGaussRange<DateTimeOffset>> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        #endregion Boilerplate
    }
}
