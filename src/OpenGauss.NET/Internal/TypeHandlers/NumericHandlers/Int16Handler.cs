using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NumericHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL smallint data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-numeric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class Int16Handler : OpenGaussSimpleTypeHandler<short>,
        IOpenGaussSimpleTypeHandler<byte>, IOpenGaussSimpleTypeHandler<sbyte>, IOpenGaussSimpleTypeHandler<int>, IOpenGaussSimpleTypeHandler<long>,
        IOpenGaussSimpleTypeHandler<float>, IOpenGaussSimpleTypeHandler<double>, IOpenGaussSimpleTypeHandler<decimal>
    {
        public Int16Handler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override short Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadInt16();

        byte IOpenGaussSimpleTypeHandler<byte>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => checked((byte)Read(buf, len, fieldDescription));

        sbyte IOpenGaussSimpleTypeHandler<sbyte>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => checked((sbyte)Read(buf, len, fieldDescription));

        int IOpenGaussSimpleTypeHandler<int>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

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
        public override int ValidateAndGetLength(short value, OpenGaussParameter? parameter) => 2;
        /// <inheritdoc />
        public int ValidateAndGetLength(byte value, OpenGaussParameter? parameter)           => 2;
        /// <inheritdoc />
        public int ValidateAndGetLength(sbyte value, OpenGaussParameter? parameter)          => 2;
        /// <inheritdoc />
        public int ValidateAndGetLength(decimal value, OpenGaussParameter? parameter)        => 2;

        /// <inheritdoc />
        public int ValidateAndGetLength(int value, OpenGaussParameter? parameter)
        {
            _ = checked((short)value);
            return 2;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(long value, OpenGaussParameter? parameter)
        {
            _ = checked((short)value);
            return 2;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(float value, OpenGaussParameter? parameter)
        {
            _ = checked((short)value);
            return 2;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(double value, OpenGaussParameter? parameter)
        {
            _ = checked((short)value);
            return 2;
        }

        /// <inheritdoc />
        public override void Write(short value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteInt16(value);
        /// <inheritdoc />
        public void Write(int value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)            => buf.WriteInt16((short)value);
        /// <inheritdoc />
        public void Write(long value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)           => buf.WriteInt16((short)value);
        /// <inheritdoc />
        public void Write(byte value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)           => buf.WriteInt16(value);
        /// <inheritdoc />
        public void Write(sbyte value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)          => buf.WriteInt16(value);
        /// <inheritdoc />
        public void Write(decimal value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)        => buf.WriteInt16((short)value);
        /// <inheritdoc />
        public void Write(double value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)         => buf.WriteInt16((short)value);
        /// <inheritdoc />
        public void Write(float value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)          => buf.WriteInt16((short)value);

        #endregion Write
    }
}
