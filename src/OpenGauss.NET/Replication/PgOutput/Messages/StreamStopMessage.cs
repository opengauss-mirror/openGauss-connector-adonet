using OpenGauss.NET.Types;
using System;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol stream stop message
    /// </summary>
    public sealed class StreamStopMessage : PgOutputReplicationMessage
    {
        internal StreamStopMessage() {}

        internal new StreamStopMessage Populate(OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock)
        {
            base.Populate(walStart, walEnd, serverClock);
            return this;
        }
    }
}
