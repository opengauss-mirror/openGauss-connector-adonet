using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL bool data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-boolean.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class BoolHandler : OpenGaussSimpleTypeHandler<bool>
    {
        public BoolHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override bool Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadByte() != 0;

        /// <inheritdoc />
        public override int ValidateAndGetLength(bool value, OpenGaussParameter? parameter)
            => 1;

        /// <inheritdoc />
        public override void Write(bool value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteByte(value ? (byte)1 : (byte)0);
    }
}
