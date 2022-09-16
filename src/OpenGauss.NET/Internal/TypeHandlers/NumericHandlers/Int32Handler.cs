using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NumericHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL integer data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-numeric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class Int32Handler : OpenGaussSimpleTypeHandler<int>, IOpenGaussSimpleTypeHandler<int>,
        IOpenGaussSimpleTypeHandler<byte>, IOpenGaussSimpleTypeHandler<short>, IOpenGaussSimpleTypeHandler<long>,
        IOpenGaussSimpleTypeHandler<float>, IOpenGaussSimpleTypeHandler<double>, IOpenGaussSimpleTypeHandler<decimal>
    {
        public Int32Handler(PostgresType pgType) : base(pgType) {}

        #region Read

        public override int Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadInt32();

        byte IOpenGaussSimpleTypeHandler<byte>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => checked((byte)Read(buf, len, fieldDescription));

        short IOpenGaussSimpleTypeHandler<short>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => checked((short)Read(buf, len, fieldDescription));

        long IOpenGaussSimpleTypeHandler<long>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

        float IOpenGaussSimpleTypeHandler<float>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

        double IOpenGaussSimpleTypeHandler<double>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

        decimal IOpenGaussSimpleTypeHandler<decimal>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

        #endregion Read

        #region Write

        /// <inheritdoc />
        public override int ValidateAndGetLength(int value, OpenGaussParameter? parameter) => 4;
        /// <inheritdoc />
        public int ValidateAndGetLength(short value, OpenGaussParameter? parameter)        => 4;
        /// <inheritdoc />
        public int ValidateAndGetLength(byte value, OpenGaussParameter? parameter)         => 4;
        /// <inheritdoc />
        public int ValidateAndGetLength(decimal value, OpenGaussParameter? parameter)      => 4;

        /// <inheritdoc />
        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
        {
            _ = checked((int)value);
            return 4;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(float value, OpenGaussParameter? parameter)
        {
            _ = checked((int)value);
            return 4;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(double value, OpenGaussParameter? parameter)
        {
            _ = checked((int)value);
            return 4;
        }

        /// <inheritdoc />
        public override void Write(int value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteInt32(value);
        /// <inheritdoc />
        public void Write(short value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)        => buf.WriteInt32(value);
        /// <inheritdoc />
        public void Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)         => buf.WriteInt32((int)value);
        /// <inheritdoc />
        public void Write(byte value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)         => buf.WriteInt32(value);
        /// <inheritdoc />
        public void Write(float value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)        => buf.WriteInt32((int)value);
        /// <inheritdoc />
        public void Write(double value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)       => buf.WriteInt32((int)value);
        /// <inheritdoc />
        public void Write(decimal value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)      => buf.WriteInt32((int)value);

        #endregion Write
    }
}
