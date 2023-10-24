using System;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;
using BclIntervalHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.IntervalHandler;

#pragma warning disable 618 // OpenGaussTimeSpan is obsolete, remove in 7.0

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class IntervalHandler :
        OpenGaussSimpleTypeHandler<Period>,
        IOpenGaussSimpleTypeHandler<Duration>,
        IOpenGaussSimpleTypeHandler<OpenGaussTimeSpan>,
        IOpenGaussSimpleTypeHandler<TimeSpan>,
        IOpenGaussSimpleTypeHandler<OpenGaussInterval>
    {
        readonly BclIntervalHandler _bclHandler;

        internal IntervalHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclIntervalHandler(postgresType);

        public override Period Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            var microsecondsInDay = buf.ReadInt64();
            var days = buf.ReadInt32();
            var totalMonths = buf.ReadInt32();

            // NodaTime will normalize most things (i.e. nanoseconds to milliseconds, seconds...)
            // but it will not normalize months to years.
            var months = totalMonths % 12;
            var years = totalMonths / 12;

            return new PeriodBuilder
            {
                Nanoseconds = microsecondsInDay * 1000,
                Days = days,
                Months = months,
                Years = years
            }.Build().Normalize();
        }

        public override int ValidateAndGetLength(Period value, OpenGaussParameter? parameter)
            => 16;

        public override void Write(Period value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            // Note that the end result must be long
            // see #3438
            var microsecondsInDay =
                (((value.Hours * NodaConstants.MinutesPerHour + value.Minutes) * NodaConstants.SecondsPerMinute + value.Seconds) * NodaConstants.MillisecondsPerSecond + value.Milliseconds) * 1000 +
                value.Nanoseconds / 1000; // Take the microseconds, discard the nanosecond remainder

            buf.WriteInt64(microsecondsInDay);
            buf.WriteInt32(value.Weeks * 7 + value.Days); // days
            buf.WriteInt32(value.Years * 12 + value.Months); // months
        }

        Duration IOpenGaussSimpleTypeHandler<Duration>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
        {
            var microsecondsInDay = buf.ReadInt64();
            var days = buf.ReadInt32();
            var totalMonths = buf.ReadInt32();

            if (totalMonths != 0)
                throw new OpenGaussException("Cannot read PostgreSQL interval with non-zero months to NodaTime Duration. Try reading as a NodaTime Period instead.");

            return Duration.FromDays(days) + Duration.FromNanoseconds(microsecondsInDay * 1000);
        }

        public int ValidateAndGetLength(Duration value, OpenGaussParameter? parameter) => 16;

        public void Write(Duration value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            const long microsecondsPerSecond = 1_000_000;

            // Note that the end result must be long
            // see #3438
            var microsecondsInDay =
                (((value.Hours * NodaConstants.MinutesPerHour + value.Minutes) * NodaConstants.SecondsPerMinute + value.Seconds) *
                    microsecondsPerSecond + value.SubsecondNanoseconds / 1000); // Take the microseconds, discard the nanosecond remainder

            buf.WriteInt64(microsecondsInDay);
            buf.WriteInt32(value.Days); // days
            buf.WriteInt32(0); // months
        }

        OpenGaussTimeSpan IOpenGaussSimpleTypeHandler<OpenGaussTimeSpan>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<OpenGaussTimeSpan>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<OpenGaussTimeSpan>.ValidateAndGetLength(OpenGaussTimeSpan value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<OpenGaussTimeSpan>.Write(OpenGaussTimeSpan value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

        TimeSpan IOpenGaussSimpleTypeHandler<TimeSpan>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<TimeSpan>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<TimeSpan>.ValidateAndGetLength(TimeSpan value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<TimeSpan>)_bclHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<TimeSpan>.Write(TimeSpan value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<TimeSpan>)_bclHandler).Write(value, buf, parameter);

        OpenGaussInterval IOpenGaussSimpleTypeHandler<OpenGaussInterval>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<OpenGaussInterval>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<OpenGaussInterval>.ValidateAndGetLength(OpenGaussInterval value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<OpenGaussInterval>)_bclHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<OpenGaussInterval>.Write(OpenGaussInterval value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<OpenGaussInterval>)_bclHandler).Write(value, buf, parameter);
    }
}
