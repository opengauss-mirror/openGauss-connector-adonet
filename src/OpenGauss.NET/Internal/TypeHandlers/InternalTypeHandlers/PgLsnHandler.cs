using System.Diagnostics;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers.InternalTypeHandlers
{
    partial class PgLsnHandler : OpenGaussSimpleTypeHandler<OpenGaussLogSequenceNumber>
    {
        public PgLsnHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        public override OpenGaussLogSequenceNumber Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            Debug.Assert(len == 8);
            return new OpenGaussLogSequenceNumber(buf.ReadUInt64());
        }

        #endregion Read

        #region Write

        public override int ValidateAndGetLength(OpenGaussLogSequenceNumber value, OpenGaussParameter? parameter) => 8;

        public override void Write(OpenGaussLogSequenceNumber value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteUInt64((ulong)value);

        #endregion Write
    }
}
