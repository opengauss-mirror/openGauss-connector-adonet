using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol update message for tables with REPLICA IDENTITY set to FULL.
    /// </summary>
    public sealed class FullUpdateMessage : UpdateMessage
    {
        readonly ReplicationTuple _oldRow;
        readonly SecondRowTupleEnumerable _newRow;

        /// <summary>
        /// Columns representing the old row.
        /// </summary>
        public ReplicationTuple OldRow => _oldRow;

        /// <summary>
        /// Columns representing the new row.
        /// </summary>
        public override ReplicationTuple NewRow => _newRow;

        internal FullUpdateMessage(OpenGaussConnector connector)
        {
            _oldRow = new(connector);
            _newRow = new(connector, _oldRow);
        }

        internal UpdateMessage Populate(
            OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock, uint? transactionXid,
            RelationMessage relation, ushort numColumns)
        {
            base.Populate(walStart, walEnd, serverClock, transactionXid, relation);

            _oldRow.Reset(numColumns, relation.RowDescription);
            _newRow.Reset(numColumns, relation.RowDescription);

            return this;
        }

        internal Task Consume(CancellationToken cancellationToken)
            => _newRow.Consume(cancellationToken);
    }
}
