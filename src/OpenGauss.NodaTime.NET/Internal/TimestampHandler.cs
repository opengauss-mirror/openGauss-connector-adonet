using System;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using BclTimestampHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.TimestampHandler;
using static OpenGauss.NodaTime.NET.Internal.NodaTimeUtils;

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class TimestampHandler : OpenGaussSimpleTypeHandler<LocalDateTime>,
        IOpenGaussSimpleTypeHandler<DateTime>, IOpenGaussSimpleTypeHandler<long>
    {
        readonly BclTimestampHandler _bclHandler;

        const string InfinityExceptionMessage = "Can't read infinity value since OpenGauss.DisableDateTimeInfinityConversions is enabled";

        internal TimestampHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclTimestampHandler(postgresType);

        #region Read

        public override LocalDateTime Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => ReadLocalDateTime(buf);

        // TODO: Switch to use LocalDateTime.MinMaxValue when available (#4061)
        internal static LocalDateTime ReadLocalDateTime(OpenGaussReadBuffer buf)
            => buf.ReadInt64() switch
            {
                long.MaxValue => DisableDateTimeInfinityConversions
                    ? throw new InvalidCastException(InfinityExceptionMessage)
                    : LocalDate.MaxIsoValue + LocalTime.MaxValue,
                long.MinValue => DisableDateTimeInfinityConversions
                    ? throw new InvalidCastException(InfinityExceptionMessage)
                    : LocalDate.MinIsoValue + LocalTime.MinValue,
                var value => DecodeInstant(value).InUtc().LocalDateTime
            };

        DateTime IOpenGaussSimpleTypeHandler<DateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read(buf, len, fieldDescription);

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Read(buf, len, fieldDescription);

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(LocalDateTime value, OpenGaussParameter? parameter)
            => 8;

        public override void Write(LocalDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => WriteLocalDateTime(value, buf);

        internal static void WriteLocalDateTime(LocalDateTime value, OpenGaussWriteBuffer buf)
        {
            // TODO: Switch to use LocalDateTime.MinMaxValue when available (#4061)
            if (!DisableDateTimeInfinityConversions)
            {
                if (value == LocalDate.MaxIsoValue + LocalTime.MaxValue)
                {
                    buf.WriteInt64(long.MaxValue);
                    return;
                }

                if (value == LocalDate.MinIsoValue + LocalTime.MinValue)
                {
                    buf.WriteInt64(long.MinValue);
                    return;
                }
            }

            buf.WriteInt64(EncodeInstant(value.InUtc().ToInstant()));
        }

        public int ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_bclHandler).ValidateAndGetLength(value, parameter);

        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTime>.Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_bclHandler).Write(value, buf, parameter);

        void IOpenGaussSimpleTypeHandler<long>.Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_bclHandler).Write(value, buf, parameter);

        #endregion Write
    }
}
