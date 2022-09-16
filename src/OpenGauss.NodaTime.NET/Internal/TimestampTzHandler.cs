using System;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using BclTimestampTzHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.TimestampTzHandler;
using static OpenGauss.NodaTime.NET.Internal.NodaTimeUtils;

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class TimestampTzHandler : OpenGaussSimpleTypeHandler<Instant>, IOpenGaussSimpleTypeHandler<ZonedDateTime>,
        IOpenGaussSimpleTypeHandler<OffsetDateTime>, IOpenGaussSimpleTypeHandler<DateTimeOffset>,
        IOpenGaussSimpleTypeHandler<DateTime>, IOpenGaussSimpleTypeHandler<long>
    {
        readonly BclTimestampTzHandler _bclHandler;

        const string InfinityExceptionMessage = "Can't read infinity value since OpenGauss.DisableDateTimeInfinityConversions is enabled";

        public TimestampTzHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclTimestampTzHandler(postgresType);

        #region Read

        public override Instant Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => ReadInstant(buf);

        internal static Instant ReadInstant(OpenGaussReadBuffer buf)
            => buf.ReadInt64() switch
            {
                long.MaxValue => DisableDateTimeInfinityConversions ? throw new InvalidCastException(InfinityExceptionMessage) : Instant.MaxValue,
                long.MinValue => DisableDateTimeInfinityConversions ? throw new InvalidCastException(InfinityExceptionMessage) : Instant.MinValue,
                var value => DecodeInstant(value)
            };

        ZonedDateTime IOpenGaussSimpleTypeHandler<ZonedDateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription).InUtc();

        OffsetDateTime IOpenGaussSimpleTypeHandler<OffsetDateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription).WithOffset(Offset.Zero);

        DateTimeOffset IOpenGaussSimpleTypeHandler<DateTimeOffset>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<DateTimeOffset>(buf, len, fieldDescription);

        DateTime IOpenGaussSimpleTypeHandler<DateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<DateTime>(buf, len, fieldDescription);

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Read(buf, len, fieldDescription);

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(Instant value, OpenGaussParameter? parameter)
            => 8;

        int IOpenGaussSimpleTypeHandler<ZonedDateTime>.ValidateAndGetLength(ZonedDateTime value, OpenGaussParameter? parameter)
            => value.Zone == DateTimeZone.Utc || LegacyTimestampBehavior
                ? 8
                : throw new InvalidCastException(
                    $"Cannot write ZonedDateTime with Zone={value.Zone} to PostgreSQL type 'timestamp with time zone', " +
                    "only UTC is supported. " +
                    "See the OpenGauss.EnableLegacyTimestampBehavior AppContext switch to enable legacy behavior.");

        public int ValidateAndGetLength(OffsetDateTime value, OpenGaussParameter? parameter)
            => value.Offset == Offset.Zero || LegacyTimestampBehavior
                ? 8
                : throw new InvalidCastException(
                    $"Cannot write OffsetDateTime with Offset={value.Offset} to PostgreSQL type 'timestamp with time zone', " +
                    "only offset 0 (UTC) is supported. " +
                    "See the OpenGauss.EnableLegacyTimestampBehavior AppContext switch to enable legacy behavior.");

        public override void Write(Instant value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => WriteInstant(value, buf);

        internal static void WriteInstant(Instant value, OpenGaussWriteBuffer buf)
        {
            if (!DisableDateTimeInfinityConversions)
            {
                if (value == Instant.MaxValue)
                {
                    buf.WriteInt64(long.MaxValue);
                    return;
                }

                if (value == Instant.MinValue)
                {
                    buf.WriteInt64(long.MinValue);
                    return;
                }
            }

            buf.WriteInt64(EncodeInstant(value));
        }

        void IOpenGaussSimpleTypeHandler<ZonedDateTime>.Write(ZonedDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => Write(value.ToInstant(), buf, parameter);

        public void Write(OffsetDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => Write(value.ToInstant(), buf, parameter);

        int IOpenGaussSimpleTypeHandler<DateTimeOffset>.ValidateAndGetLength(DateTimeOffset value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTimeOffset>.Write(DateTimeOffset value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

        int IOpenGaussSimpleTypeHandler<DateTime>.ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_bclHandler).ValidateAndGetLength(value, parameter);

        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTime>.Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

        void IOpenGaussSimpleTypeHandler<long>.Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Write(value, buf, parameter);

        #endregion Write
    }
}
