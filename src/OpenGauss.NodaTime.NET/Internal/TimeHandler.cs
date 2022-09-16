using System;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using BclTimeHandler = OpenGauss.NET.Internal.TypeHandlers.DateTimeHandlers.TimeHandler;

namespace OpenGauss.NodaTime.NET.Internal
{
    sealed partial class TimeHandler : OpenGaussSimpleTypeHandler<LocalTime>, IOpenGaussSimpleTypeHandler<TimeSpan>
#if NET6_0_OR_GREATER
        , IOpenGaussSimpleTypeHandler<TimeOnly>
#endif
    {
        readonly BclTimeHandler _bclHandler;

        internal TimeHandler(PostgresType postgresType)
            : base(postgresType)
            => _bclHandler = new BclTimeHandler(postgresType);

        // PostgreSQL time resolution == 1 microsecond == 10 ticks
        public override LocalTime Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => LocalTime.FromTicksSinceMidnight(buf.ReadInt64() * 10);

        public override int ValidateAndGetLength(LocalTime value, OpenGaussParameter? parameter)
            => 8;

        public override void Write(LocalTime value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteInt64(value.TickOfDay / 10);

        TimeSpan IOpenGaussSimpleTypeHandler<TimeSpan>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
           => _bclHandler.Read<TimeSpan>(buf, len, fieldDescription);

        int IOpenGaussSimpleTypeHandler<TimeSpan>.ValidateAndGetLength(TimeSpan value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        void IOpenGaussSimpleTypeHandler<TimeSpan>.Write(TimeSpan value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);

#if NET6_0_OR_GREATER
        TimeOnly IOpenGaussSimpleTypeHandler<TimeOnly>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => _bclHandler.Read<TimeOnly>(buf, len, fieldDescription);

        public int ValidateAndGetLength(TimeOnly value, OpenGaussParameter? parameter)
            => _bclHandler.ValidateAndGetLength(value, parameter);

        public void Write(TimeOnly value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => _bclHandler.Write(value, buf, parameter);
#endif
    }
}
