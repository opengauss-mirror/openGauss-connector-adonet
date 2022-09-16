using OpenGauss.NET.Types;
using System;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol origin message
    /// </summary>
    public sealed class OriginMessage : PgOutputReplicationMessage
    {
        /// <summary>
        /// The LSN of the commit on the origin server.
        /// </summary>
        public OpenGaussLogSequenceNumber OriginCommitLsn { get; private set; }

        /// <summary>
        /// Name of the origin.
        /// </summary>
        public string OriginName { get; private set; } = string.Empty;

        internal OriginMessage() {}

        internal OriginMessage Populate(
            OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock, OpenGaussLogSequenceNumber originCommitLsn,
            string originName)
        {
            base.Populate(walStart, walEnd, serverClock);

            OriginCommitLsn = originCommitLsn;
            OriginName = originName;

            return this;
        }
    }
}
