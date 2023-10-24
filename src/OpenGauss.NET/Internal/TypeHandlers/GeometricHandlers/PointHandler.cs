using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers.GeometricHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL point data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class PointHandler : OpenGaussSimpleTypeHandler<OpenGaussPoint>
    {
        public PointHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override OpenGaussPoint Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => new(buf.ReadDouble(), buf.ReadDouble());

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussPoint value, OpenGaussParameter? parameter)
            => 16;

        /// <inheritdoc />
        public override void Write(OpenGaussPoint value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteDouble(value.X);
            buf.WriteDouble(value.Y);
        }
    }
}
