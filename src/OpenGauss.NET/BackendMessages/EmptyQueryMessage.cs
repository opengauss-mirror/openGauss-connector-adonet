namespace OpenGauss.NET.BackendMessages
{
    class EmptyQueryMessage : IBackendMessage
    {
        public BackendMessageCode Code => BackendMessageCode.EmptyQueryResponse;
        internal static readonly EmptyQueryMessage Instance = new();
        EmptyQueryMessage() { }
    }
}
