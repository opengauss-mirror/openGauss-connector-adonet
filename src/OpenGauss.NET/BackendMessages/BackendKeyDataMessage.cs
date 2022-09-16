using OpenGauss.NET.Internal;

namespace OpenGauss.NET.BackendMessages
{
    class BackendKeyDataMessage : IBackendMessage
    {
        public BackendMessageCode Code => BackendMessageCode.BackendKeyData;

        internal int BackendProcessId { get; private set; }
        internal int BackendSecretKey { get; private set; }

        internal BackendKeyDataMessage(OpenGaussReadBuffer buf)
        {
            BackendProcessId = buf.ReadInt32();
            BackendSecretKey = buf.ReadInt32();
        }
    }
}
