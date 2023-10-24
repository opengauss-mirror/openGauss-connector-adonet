using System.Diagnostics;
using System.Net.NetworkInformation;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.NetworkHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL macaddr and macaddr8 data types.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-net-types.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class MacaddrHandler : OpenGaussSimpleTypeHandler<PhysicalAddress>
    {
        public MacaddrHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override PhysicalAddress Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
        {
            Debug.Assert(len == 6 || len == 8);

            var bytes = new byte[len];

            buf.ReadBytes(bytes, 0, len);
            return new PhysicalAddress(bytes);
        }

        #endregion Read

        #region Write

        /// <inheritdoc />
        public override int ValidateAndGetLength(PhysicalAddress value, OpenGaussParameter? parameter)
            => value.GetAddressBytes().Length;

        /// <inheritdoc />
        public override void Write(PhysicalAddress value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
        {
            var bytes = value.GetAddressBytes();
            buf.WriteBytes(bytes, 0, bytes.Length);
        }

        #endregion Write
    }
}
