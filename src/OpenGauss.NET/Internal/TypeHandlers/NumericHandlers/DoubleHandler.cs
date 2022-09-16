using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NumericHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL double precision data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-numeric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class DoubleHandler : OpenGaussSimpleTypeHandler<double>
    {
        public DoubleHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override double Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => buf.ReadDouble();

        /// <inheritdoc />
        public override int ValidateAndGetLength(double value, OpenGaussParameter? parameter)
            => 8;

        /// <inheritdoc />
        public override void Write(double value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => buf.WriteDouble(value);
    }
}
