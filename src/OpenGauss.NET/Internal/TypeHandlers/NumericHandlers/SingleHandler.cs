using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NumericHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL real data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-numeric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class SingleHandler : OpenGaussSimpleTypeHandler<float>, IOpenGaussSimpleTypeHandler<double>
    {
        public SingleHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override float Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadSingle();

        double IOpenGaussSimpleTypeHandler<double>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
            => Read(buf, len, fieldDescription);

        #endregion Read

        #region Write

        /// <inheritdoc />
        public int ValidateAndGetLength(double value, OpenGaussParameter? parameter)         => 4;
        /// <inheritdoc />
        public override int ValidateAndGetLength(float value, OpenGaussParameter? parameter) => 4;

        /// <inheritdoc />
        public void Write(double value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)         => buf.WriteSingle((float)value);
        /// <inheritdoc />
        public override void Write(float value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter) => buf.WriteSingle(value);

        #endregion Write
    }
}
