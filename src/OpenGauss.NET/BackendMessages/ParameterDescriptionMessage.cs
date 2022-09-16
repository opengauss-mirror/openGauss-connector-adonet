using System.Collections.Generic;
using OpenGauss.NET.Internal;

namespace OpenGauss.NET.BackendMessages
{
    class ParameterDescriptionMessage : IBackendMessage
    {
        // ReSharper disable once InconsistentNaming
        internal List<uint> TypeOIDs { get; }

        internal ParameterDescriptionMessage()
        {
            TypeOIDs = new List<uint>();
        }

        internal ParameterDescriptionMessage Load(OpenGaussReadBuffer buf)
        {
            var numParams = buf.ReadUInt16();
            TypeOIDs.Clear();
            for (var i = 0; i < numParams; i++)
                TypeOIDs.Add(buf.ReadUInt32());
            return this;
        }

        public BackendMessageCode Code => BackendMessageCode.ParameterDescription;
    }
}
