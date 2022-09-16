using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.InternalTypeHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL "char" type, used only internally.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-character.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class InternalCharHandler : OpenGaussSimpleTypeHandler<char>,
        IOpenGaussSimpleTypeHandler<byte>, IOpenGaussSimpleTypeHandler<short>, IOpenGaussSimpleTypeHandler<int>, IOpenGaussSimpleTypeHandler<long>
    {
        public InternalCharHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override char Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => (char)buf.ReadByte();

        byte IOpenGaussSimpleTypeHandler<byte>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadByte();

        short IOpenGaussSimpleTypeHandler<short>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadByte();

        int IOpenGaussSimpleTypeHandler<int>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadByte();

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => buf.ReadByte();

        #endregion

        #region Write

        /// <inheritdoc />
        public int ValidateAndGetLength(byte value, OpenGaussParameter? parameter)          => 1;

        /// <inheritdoc />
        public override int ValidateAndGetLength(char value, OpenGaussParameter? parameter)
        {
            _ = checked((byte)value);
            return 1;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(short value, OpenGaussParameter? parameter)
        {
            _ = checked((byte)value);
            return 1;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(int value, OpenGaussParameter? parameter)
        {
            _ = checked((byte)value);
            return 1;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
        {
            _ = checked((byte)value);
            return 1;
        }

        /// <inheritdoc />
        public override void Write(char value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteByte((byte)value);
        /// <inheritdoc />
        public void Write(byte value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteByte(value);
        /// <inheritdoc />
        public void Write(short value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteByte((byte)value);
        /// <inheritdoc />
        public void Write(int value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteByte((byte)value);
        /// <inheritdoc />
        public void Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteByte((byte)value);

        #endregion
    }
}
