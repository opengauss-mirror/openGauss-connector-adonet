using OpenGauss.NET.Types;
using System;

namespace OpenGauss.NET.Replication
{
    /// <summary>
    /// The common base class for all streaming replication messages
    /// </summary>
    public abstract class ReplicationMessage
    {
        /// <summary>
        /// The starting point of the WAL data in this message.
        /// </summary>
        public OpenGaussLogSequenceNumber WalStart { get; private set; }

        /// <summary>
        /// The current end of WAL on the server.
        /// </summary>
        public OpenGaussLogSequenceNumber WalEnd { get; private set; }

        /// <summary>
        /// The server's system clock at the time this message was transmitted, as microseconds since midnight on 2000-01-01.
        /// </summary>
        public DateTime ServerClock { get; private set; }

        private protected void Populate(OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock)
        {
            WalStart = walStart;
            WalEnd = walEnd;
            ServerClock = serverClock;
        }
    }
}
