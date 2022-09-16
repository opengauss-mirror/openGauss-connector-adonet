using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NumericHandlers
{
    /// <summary>
    /// A type handler for PostgreSQL unsigned 32-bit data types. This is only used for internal types.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-oid.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class UInt32Handler : OpenGaussSimpleTypeHandler<uint>
    {
        public UInt32Handler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override uint Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadUInt32();

        /// <inheritdoc />
        public override int ValidateAndGetLength(uint value, OpenGaussParameter? parameter) => 4;

        /// <inheritdoc />
        public override void Write(uint value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteUInt32(value);
    }
}
