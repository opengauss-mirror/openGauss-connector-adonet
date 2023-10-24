using System.Net;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

#pragma warning disable 618

namespace OpenGauss.NET.Internal.TypeHandlers.NetworkHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL cidr data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-net-types.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class CidrHandler : OpenGaussSimpleTypeHandler<(IPAddress Address, int Subnet)>, IOpenGaussSimpleTypeHandler<OpenGaussInet>
    {
        public CidrHandler(PostgresType pgType) : base(pgType) {}

        /// <inheritdoc />
        public override (IPAddress Address, int Subnet) Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => InetHandler.DoRead(buf, len, fieldDescription, true);

        OpenGaussInet IOpenGaussSimpleTypeHandler<OpenGaussInet>.Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription)
        {
            var (address, subnet) = Read(buf, len, fieldDescription);
            return new OpenGaussInet(address, subnet);
        }

        /// <inheritdoc />
        public override int ValidateAndGetLength((IPAddress Address, int Subnet) value, OpenGaussParameter? parameter)
            => InetHandler.GetLength(value.Address);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussInet value, OpenGaussParameter? parameter)
            => InetHandler.GetLength(value.Address);

        /// <inheritdoc />
        public override void Write((IPAddress Address, int Subnet) value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => InetHandler.DoWrite(value.Address, value.Subnet, buf, true);

        /// <inheritdoc />
        public void Write(OpenGaussInet value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => InetHandler.DoWrite(value.Address, value.Netmask, buf, true);
    }
}
