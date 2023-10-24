using System;
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
    public partial class TimestampTzRangeHandler : RangeHandler<Instant>,
        IOpenGaussTypeHandler<Interval>, IOpenGaussTypeHandler<OpenGaussRange<ZonedDateTime>>, IOpenGaussTypeHandler<OpenGaussRange<OffsetDateTime>>,
        IOpenGaussTypeHandler<OpenGaussRange<DateTime>>, IOpenGaussTypeHandler<OpenGaussRange<DateTimeOffset>>
    {
        public override Type GetFieldType(FieldDescription? fieldDescription = null) => typeof(Interval);
        public override Type GetProviderSpecificFieldType(FieldDescription? fieldDescription = null) => typeof(Interval);

        public TimestampTzRangeHandler(PostgresType rangePostgresType, OpenGaussTypeHandler subtypeHandler)
            : base(rangePostgresType, subtypeHandler)
        {
        }

        public override async ValueTask<object> ReadAsObject(OpenGaussReadBuffer buf, int len, bool async,
            FieldDescription? fieldDescription = null)
            => (await Read<Interval>(buf, len, async, fieldDescription))!;

        // internal Interval ConvertRangetoInterval(OpenGaussRange<Instant> range)
        async ValueTask<Interval> IOpenGaussTypeHandler<Interval>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            var range = await Read(buf, len, async, fieldDescription);

            // NodaTime Interval includes the start instant and excludes the end instant.
            Instant? start = range.LowerBoundInfinite
                ? null
                : range.LowerBoundIsInclusive
                    ? range.LowerBound
                    : range.LowerBound + Duration.Epsilon;
            Instant? end = range.UpperBoundInfinite
                ? null
                : range.UpperBoundIsInclusive
                    ? range.UpperBound + Duration.Epsilon
                    : range.UpperBound;
            return new(start, end);
        }

        public int ValidateAndGetLength(Interval value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(
                new OpenGaussRange<Instant>(value.Start, true, !value.HasStart, value.End, false, !value.HasEnd), ref lengthCache, parameter);

        public Task Write(Interval value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(new OpenGaussRange<Instant>(value.Start, true, !value.HasStart, value.End, false, !value.HasEnd),
                buf, lengthCache, parameter, async, cancellationToken);

        #region Boilerplate

        ValueTask<OpenGaussRange<ZonedDateTime>> IOpenGaussTypeHandler<OpenGaussRange<ZonedDateTime>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<ZonedDateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<OffsetDateTime>> IOpenGaussTypeHandler<OpenGaussRange<OffsetDateTime>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<OffsetDateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<DateTime>> IOpenGaussTypeHandler<OpenGaussRange<DateTime>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<DateTime>(buf, len, async, fieldDescription);

        ValueTask<OpenGaussRange<DateTimeOffset>> IOpenGaussTypeHandler<OpenGaussRange<DateTimeOffset>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<DateTimeOffset>(buf, len, async, fieldDescription);

        public int ValidateAndGetLength(OpenGaussRange<ZonedDateTime> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<OffsetDateTime> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<DateTime> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<DateTimeOffset> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        public Task Write(OpenGaussRange<ZonedDateTime> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<OffsetDateTime> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<DateTime> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(OpenGaussRange<DateTimeOffset> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        #endregion Boilerplate
    }
}
