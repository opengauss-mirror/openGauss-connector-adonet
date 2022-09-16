using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers.GeometricHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL line data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class LineHandler : OpenGaussSimpleTypeHandler<OpenGaussLine>
    {
        public LineHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override OpenGaussLine Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => new(buf.ReadDouble(), buf.ReadDouble(), buf.ReadDouble());

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussLine value, OpenGaussParameter? parameter)
            => 24;

        /// <inheritdoc />
        public override void Write(OpenGaussLine value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteDouble(value.A);
            buf.WriteDouble(value.B);
            buf.WriteDouble(value.C);
        }
    }
}
