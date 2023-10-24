using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers.GeometricHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL box data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class BoxHandler : OpenGaussSimpleTypeHandler<OpenGaussBox>
    {
        public BoxHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override OpenGaussBox Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => new(
                new OpenGaussPoint(buf.ReadDouble(), buf.ReadDouble()),
                new OpenGaussPoint(buf.ReadDouble(), buf.ReadDouble())
            );

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussBox value, OpenGaussParameter? parameter)
            => 32;

        /// <inheritdoc />
        public override void Write(OpenGaussBox value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            buf.WriteDouble(value.Right);
            buf.WriteDouble(value.Top);
            buf.WriteDouble(value.Left);
            buf.WriteDouble(value.Bottom);
        }
    }
}
