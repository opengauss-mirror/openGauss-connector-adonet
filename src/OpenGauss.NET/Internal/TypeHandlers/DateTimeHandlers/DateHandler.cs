using System;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;
using static OpenGauss.NET.Util.Statics;

#pragma warning disable 618 // OpenGaussDate is obsolete, remove in 7.0

namespace OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL date data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-datetime.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class DateHandler : OpenGaussSimpleTypeHandlerWithPsv<DateTime, OpenGaussDate>,
        IOpenGaussSimpleTypeHandler<int>
#if NET6_0_OR_GREATER
        , IOpenGaussSimpleTypeHandler<DateOnly>
#endif
    {
        /// <summary>
        /// Constructs a <see cref="DateHandler"/>
        /// </summary>
        public DateHandler(PostgresType postgresType) : base(postgresType) {}

        #region Read

        /// <inheritdoc />
        public override DateTime Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            var opengaussDate = ReadPsv(buf, len, fieldDescription);

            if (opengaussDate.IsFinite)
                return (DateTime)opengaussDate;
            if (DisableDateTimeInfinityConversions)
                throw new InvalidCastException("Can't convert infinite date values to DateTime");
            if (opengaussDate.IsInfinity)
                return DateTime.MaxValue;
            return DateTime.MinValue;
        }

        /// <remarks>
        /// Copied wholesale from Postgresql backend/utils/adt/datetime.c:j2date
        /// </remarks>
        protected override OpenGaussDate ReadPsv(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            var binDate = buf.ReadInt32();

            return binDate switch
            {
                int.MaxValue => OpenGaussDate.Infinity,
                int.MinValue => OpenGaussDate.NegativeInfinity,
                _            => new OpenGaussDate(binDate + 730119)
            };
        }

        int IOpenGaussSimpleTypeHandler<int>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadInt32();

        #endregion Read

        #region Write

        /// <inheritdoc />
        public override int ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter) => 4;

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussDate value, OpenGaussParameter? parameter) => 4;

        /// <inheritdoc />
        public int ValidateAndGetLength(int value, OpenGaussParameter? parameter) => 4;

        /// <inheritdoc />
        public override void Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            if (!DisableDateTimeInfinityConversions)
            {
                if (value == DateTime.MaxValue)
                {
                    Write(OpenGaussDate.Infinity, buf, parameter);
                    return;
                }

                if (value == DateTime.MinValue)
                {
                    Write(OpenGaussDate.NegativeInfinity, buf, parameter);
                    return;
                }
            }

            Write(new OpenGaussDate(value), buf, parameter);
        }

        /// <inheritdoc />
        public override void Write(OpenGaussDate value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            if (value == OpenGaussDate.NegativeInfinity)
                buf.WriteInt32(int.MinValue);
            else if (value == OpenGaussDate.Infinity)
                buf.WriteInt32(int.MaxValue);
            else
                buf.WriteInt32(value.DaysSinceEra - 730119);
        }

        /// <inheritdoc />
        public void Write(int value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteInt32(value);

        #endregion Write

#if NET6_0_OR_GREATER
        DateOnly IOpenGaussSimpleTypeHandler<DateOnly>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
        {
            var opengaussDate = ReadPsv(buf, len, fieldDescription);

            if (opengaussDate.IsFinite)
                return (DateOnly)opengaussDate;
            if (DisableDateTimeInfinityConversions)
                throw new InvalidCastException("Can't convert infinite date values to DateOnly");
            if (opengaussDate.IsInfinity)
                return DateOnly.MaxValue;
            return DateOnly.MinValue;
        }

        public int ValidateAndGetLength(DateOnly value, OpenGaussParameter? parameter) => 4;

        public void Write(DateOnly value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            if (!DisableDateTimeInfinityConversions)
            {
                if (value == DateOnly.MaxValue)
                {
                    Write(OpenGaussDate.Infinity, buf, parameter);
                    return;
                }

                if (value == DateOnly.MinValue)
                {
                    Write(OpenGaussDate.NegativeInfinity, buf, parameter);
                    return;
                }
            }

            Write(new OpenGaussDate(value), buf, parameter);
        }

        public override OpenGaussTypeHandler CreateRangeHandler(PostgresType pgRangeType)
            => new RangeHandler<DateTime, DateOnly>(pgRangeType, this);

        public override OpenGaussTypeHandler CreateMultirangeHandler(PostgresMultirangeType pgRangeType)
            => new MultirangeHandler<DateTime, DateOnly>(pgRangeType, new RangeHandler<DateTime, DateOnly>(pgRangeType, this));
#endif
    }
}
