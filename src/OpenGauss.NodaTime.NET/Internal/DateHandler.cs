using System;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;
using static OpenGauss.NodaTime.NET.Internal.NodaTimeUtils;
using BclDateHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.DateHandler;

#pragma warning disable 618 // OpenGaussDate is obsolete, remove in 7.0

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class DateHandler : OpenGaussSimpleTypeHandler<LocalDate>,
        IOpenGaussSimpleTypeHandler<DateTime>, IOpenGaussSimpleTypeHandler<OpenGaussDate>, IOpenGaussSimpleTypeHandler<int>
#if NET6_0_OR_GREATER
        , IOpenGaussSimpleTypeHandler<DateOnly>
#endif
    {
        readonly BclDateHandler _bclHandler;

        const string InfinityExceptionMessage = "Can't read infinity value since OpenGauss.DisableDateTimeInfinityConversions is enabled";

        internal DateHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclDateHandler(postgresType);

        public override LocalDate Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadInt32() switch
            {
                int.MaxValue => DisableDateTimeInfinityConversions ? throw new InvalidCastException(InfinityExceptionMessage) : LocalDate.MaxIsoValue,
                int.MinValue => DisableDateTimeInfinityConversions ? throw new InvalidCastException(InfinityExceptionMessage) : LocalDate.MinIsoValue,
                var value => new LocalDate().PlusDays(value + 730119)
            };

        public override int ValidateAndGetLength(LocalDate value, OpenGaussParameter? parameter)
            => 4;

        public override void Write(LocalDate value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            if (!DisableDateTimeInfinityConversions)
            {
                if (value == LocalDate.MaxIsoValue)
                {
                    buf.WriteInt32(int.MaxValue);
                    return;
                }
                if (value == LocalDate.MinIsoValue)
                {
                    buf.WriteInt32(int.MinValue);
                    return;
                }
            }

            var totalDaysSinceEra = Period.Between(default, value, PeriodUnits.Days).Days;
            buf.WriteInt32(totalDaysSinceEra - 730119);
        }

        OpenGaussDate IOpenGaussSimpleTypeHandler<OpenGaussDate>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<OpenGaussDate>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<OpenGaussDate>.ValidateAndGetLength(OpenGaussDate value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<OpenGaussDate>.Write(OpenGaussDate value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

        DateTime IOpenGaussSimpleTypeHandler<DateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<DateTime>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<DateTime>.ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTime>.Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

        int IOpenGaussSimpleTypeHandler<int>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<int>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<int>.ValidateAndGetLength(int value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<int>.Write(int value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

#if NET6_0_OR_GREATER
        DateOnly IOpenGaussSimpleTypeHandler<DateOnly>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<DateOnly>(buf, len, fieldDescription);

        public int ValidateAndGetLength(DateOnly value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        public void Write(DateOnly value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);
#endif

        public override OpenGaussTypeHandler CreateRangeHandler(PostgresType pgRangeType)
            => new DateRangeHandler(pgRangeType, this);
    }
}
