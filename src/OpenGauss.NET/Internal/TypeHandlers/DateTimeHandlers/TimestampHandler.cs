using System;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;
using static OpenGauss.NET.Util.Statics;
using static OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.DateTimeUtils;

#pragma warning disable 618 // OpenGaussDateTime is obsolete, remove in 7.0

namespace OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL timestamp data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-datetime.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class TimestampHandler : OpenGaussSimpleTypeHandlerWithPsv<DateTime, OpenGaussDateTime>, IOpenGaussSimpleTypeHandler<long>
    {
        /// <summary>
        /// Constructs a <see cref="TimestampHandler"/>.
        /// </summary>
        public TimestampHandler(PostgresType postgresType) : base(postgresType) {}

        #region Read

        /// <inheritdoc />
        public override DateTime Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => ReadDateTime(buf, DateTimeKind.Unspecified);

        /// <inheritdoc />
        protected override OpenGaussDateTime ReadPsv(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => ReadOpenGaussDateTime(buf, len, fieldDescription);

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadInt64();

        #endregion Read

        #region Write

        /// <inheritdoc />
        public override int ValidateAndGetLength(DateTime value, OpenGaussParameter? parameter)
            => value.Kind != DateTimeKind.Utc || LegacyTimestampBehavior
                ? 8
                : throw new InvalidCastException(
                    "Cannot write DateTime with Kind=UTC to PostgreSQL type 'timestamp without time zone', " +
                    "consider using 'timestamp with time zone'. " +
                    "Note that it's not possible to mix DateTimes with different Kinds in an array/range. " +
                    "See the OpenGauss.EnableLegacyTimestampBehavior AppContext switch to enable legacy behavior.");

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussDateTime value, OpenGaussParameter? parameter)
            => value.Kind != DateTimeKind.Utc || LegacyTimestampBehavior
                ? 8
                : throw new InvalidCastException(
                    "Cannot write DateTime with Kind=UTC to PostgreSQL type 'timestamp without time zone', " +
                    "consider using 'timestamp with time zone'. " +
                    "Note that it's not possible to mix DateTimes with different Kinds in an array/range. " +
                    "See the OpenGauss.EnableLegacyTimestampBehavior AppContext switch to enable legacy behavior.");

        /// <inheritdoc />
        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter) => 8;

        /// <inheritdoc />
        public override void Write(DateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => WriteTimestamp(value, buf);

        /// <inheritdoc />
        public override void Write(OpenGaussDateTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => WriteTimestamp(value, buf);

        /// <inheritdoc />
        public void Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteInt64(value);

        #endregion Write
    }
}
