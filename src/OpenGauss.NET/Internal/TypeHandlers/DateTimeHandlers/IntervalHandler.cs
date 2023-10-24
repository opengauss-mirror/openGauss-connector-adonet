using System;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

#pragma warning disable 618 // OpenGaussTimeSpan is obsolete, remove in 7.0

namespace OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL date interval type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-datetime.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class IntervalHandler : OpenGaussSimpleTypeHandlerWithPsv<TimeSpan, OpenGaussTimeSpan>,
        IOpenGaussSimpleTypeHandler<OpenGaussInterval>
    {
        /// <summary>
        /// Constructs an <see cref="IntervalHandler"/>
        /// </summary>
        public IntervalHandler(PostgresType postgresType) : base(postgresType) {}

        /// <inheritdoc />
        public override TimeSpan Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => (TimeSpan)((IOpenGaussSimpleTypeHandler<OpenGaussTimeSpan>)this).Read(buf, len, fieldDescription);

        /// <inheritdoc />
        protected override OpenGaussTimeSpan ReadPsv(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            var ticks = buf.ReadInt64();
            var day = buf.ReadInt32();
            var month = buf.ReadInt32();
            return new OpenGaussTimeSpan(month, day, ticks * 10);
        }

        OpenGaussInterval IOpenGaussSimpleTypeHandler<OpenGaussInterval>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
        {
            var ticks = buf.ReadInt64();
            var day = buf.ReadInt32();
            var month = buf.ReadInt32();
            return new OpenGaussInterval(month, day, ticks);
        }

        /// <inheritdoc />
        public override int ValidateAndGetLength(TimeSpan value, OpenGaussParameter? parameter) => 16;

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussTimeSpan value, OpenGaussParameter? parameter) => 16;

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussInterval value, OpenGaussParameter? parameter) => 16;

        /// <inheritdoc />
        public override void Write(OpenGaussTimeSpan value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteInt64(value.Ticks / 10); // TODO: round?
            buf.WriteInt32(value.Days);
            buf.WriteInt32(value.Months);
        }

        // TODO: Can write directly from TimeSpan
        /// <inheritdoc />
        public override void Write(TimeSpan value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => Write(value, buf, parameter);

        public void Write(OpenGaussInterval value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteInt64(value.Time);
            buf.WriteInt32(value.Days);
            buf.WriteInt32(value.Months);
        }
    }
}
