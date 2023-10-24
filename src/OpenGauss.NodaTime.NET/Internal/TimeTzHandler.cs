using System;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using BclTimeTzHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.TimeTzHandler;

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class TimeTzHandler : OpenGaussSimpleTypeHandler<OffsetTime>, IOpenGaussSimpleTypeHandler<DateTimeOffset>
    {
        readonly BclTimeTzHandler _bclHandler;

        internal TimeTzHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclTimeTzHandler(postgresType);

        // Adjust from 1 microsecond to 100ns. Time zone (in seconds) is inverted.
        public override OffsetTime Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => new(
                LocalTime.FromTicksSinceMidnight(buf.ReadInt64() * 10),
                Offset.FromSeconds(-buf.ReadInt32()));

        public override int ValidateAndGetLength(OffsetTime value, OpenGaussParameter? parameter) => 12;

        public override void Write(OffsetTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteInt64(value.TickOfDay / 10);
            buf.WriteInt32(-(int)(value.Offset.Ticks / NodaConstants.TicksPerSecond));
        }

        DateTimeOffset IOpenGaussSimpleTypeHandler<DateTimeOffset>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<DateTimeOffset>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<DateTimeOffset>.ValidateAndGetLength(DateTimeOffset value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<DateTimeOffset>.Write(DateTimeOffset value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);
    }
}
