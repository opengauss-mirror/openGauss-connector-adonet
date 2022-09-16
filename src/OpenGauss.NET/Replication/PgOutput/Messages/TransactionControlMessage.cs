using System;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// The common base class for all replication messages that set the transaction xid of a transaction
    /// </summary>
    public abstract class TransactionControlMessage : PgOutputReplicationMessage
    {
        /// <summary>
        /// Xid of the transaction.
        /// </summary>
        public uint TransactionXid { get; private set; }

        private protected void Populate(OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock, uint transactionXid)
        {
            base.Populate(walStart, walEnd, serverClock);

            TransactionXid = transactionXid;
        }
    }
}
