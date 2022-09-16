using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol update message for tables with REPLICA IDENTITY set to DEFAULT.
    /// </summary>
    public class DefaultUpdateMessage : UpdateMessage
    {
        readonly ReplicationTuple _newRow;

        /// <summary>
        /// Columns representing the new row.
        /// </summary>
        public override ReplicationTuple NewRow => _newRow;

        internal DefaultUpdateMessage(OpenGaussConnector connector)
            => _newRow = new(connector);

        internal UpdateMessage Populate(
            OpenGaussLogSequenceNumber walStart, OpenGaussLogSequenceNumber walEnd, DateTime serverClock, uint? transactionXid,
            RelationMessage relation, ushort numColumns)
        {
            base.Populate(walStart, walEnd, serverClock, transactionXid, relation);

            _newRow.Reset(numColumns, relation.RowDescription);

            return this;
        }

        internal Task Consume(CancellationToken cancellationToken)
            => _newRow.Consume(cancellationToken);
    }
}
