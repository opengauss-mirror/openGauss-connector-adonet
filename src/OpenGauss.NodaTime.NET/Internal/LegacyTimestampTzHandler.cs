using System;
using NodaTime;
using NodaTime.TimeZones;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using BclTimestampTzHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.TimestampTzHandler;

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class LegacyTimestampTzHandler : OpenGaussSimpleTypeHandler<Instant>, IOpenGaussSimpleTypeHandler<ZonedDateTime>,
                              IOpenGaussSimpleTypeHandler<OffsetDateTime>, IOpenGaussSimpleTypeHandler<DateTimeOffset>, 
                              IOpenGaussSimpleTypeHandler<DateTime>, IOpenGaussSimpleTypeHandler<long>
    {
        readonly IDateTimeZoneProvider _dateTimeZoneProvider;
        readonly TimestampTzHandler _wrappedHandler;

        public LegacyTimestampTzHandler(PostgresType postgresType)
            : base(postgresType)
        {
            _dateTimeZoneProvider = DateTimeZoneProviders.Tzdb;
            _wrappedHandler = new TimestampTzHandler(postgresType);
        }

        #region Read

        public override Instant Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => _wrappedHandler.Read(buf, len, fieldDescription);

        ZonedDateTime IOpenGaussSimpleTypeHandler<ZonedDateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
        {
            try
            {
                var zonedDateTime = ((IOpenGaussSimpleTypeHandler<ZonedDateTime>)_wrappedHandler).Read(buf, len, fieldDescription);

                var value = buf.ReadInt64();
                if (value == long.MaxValue || value == long.MinValue)
                    throw new NotSupportedException("Infinity values not supported for timestamp with time zone");
                return zonedDateTime.WithZone(_dateTimeZoneProvider[buf.Connection.Timezone]);
            }
            catch (Exception e) when (
                string.Equals(buf.Connection.Timezone, "localtime", StringComparison.OrdinalIgnoreCase) &&
                (e is TimeZoneNotFoundException || e is DateTimeZoneNotFoundException))
            {
                throw new TimeZoneNotFoundException(
                    "The special PostgreSQL timezone 'localtime' is not supported when reading values of type 'timestamp with time zone'. " +
                    "Please specify a real timezone in 'postgresql.conf' on the server, or set the 'PGTZ' environment variable on the client.",
                    e);
            }
        }

        OffsetDateTime IOpenGaussSimpleTypeHandler<OffsetDateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => ((IOpenGaussSimpleTypeHandler<ZonedDateTime>)this).Read(buf, len, fieldDescription).ToOffsetDateTime();

        DateTimeOffset IOpenGaussSimpleTypeHandler<DateTimeOffset>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _wrappedHandler.Read<DateTimeOffset>(buf, len, fieldDescription);

        DateTime IOpenGaussSimpleTypeHandler<DateTime>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _wrappedHandler.Read<DateTime>(buf, len, fieldDescription);

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _wrappedHandler.Read<long>(buf, len, fieldDescription);

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(Instant value, OpenGaussParameter? parameter)
            => 8;

        int IOpenGaussSimpleTypeHandler<ZonedDateTime>.ValidateAndGetLength(ZonedDateTime value, OpenGaussParameter? parameter)
            => 8;

        public int ValidateAndGetLength(OffsetDateTime value, OpenGaussParameter? parameter)
            => 8;

        public override void Write(Instant value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _wrappedHandler.Write(value, buf, parameter);

        void IOpenGaussSimpleTypeHandler<ZonedDateTime>.Write(ZonedDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _wrappedHandler.Write(value.ToInstant(), buf, parameter);

        public void Write(OffsetDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _wrappedHandler.Write(value.ToInstant(), buf, parameter);

        int IOpenGaussSimpleTypeHandler<DateTimeOffset>.ValidateAndGetLength(DateTimeOffset value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTimeOffset>)_wrappedHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTimeOffset>.Write(DateTimeOffset value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTimeOffset>)_wrappedHandler).Write(value, buf, parameter);

        int IOpenGaussSimpleTypeHandler<DateTime>.ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_wrappedHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTime>.Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<DateTime>)_wrappedHandler).Write(value, buf, parameter);

        int IOpenGaussSimpleTypeHandler<long>.ValidateAndGetLength(long value, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_wrappedHandler).ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<long>.Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => ((IOpenGaussSimpleTypeHandler<long>)_wrappedHandler).Write(value, buf, parameter);

        #endregion Write
    }
}
