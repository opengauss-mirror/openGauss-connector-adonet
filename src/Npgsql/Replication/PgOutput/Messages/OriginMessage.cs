﻿using NpgsqlTypes;
using System;

namespace Npgsql.Replication.PgOutput.Messages
{
    /// <summary>
    /// Logical Replication Protocol origin message
    /// </summary>
    public sealed class OriginMessage : PgOutputReplicationMessage
    {
        /// <summary>
        /// The LSN of the commit on the origin server.
        /// </summary>
        public NpgsqlLogSequenceNumber OriginCommitLsn { get; private set; }

        /// <summary>
        /// Name of the origin.
        /// </summary>
        public string OriginName { get; private set; } = string.Empty;

        internal OriginMessage() {}

        internal OriginMessage Populate(
            NpgsqlLogSequenceNumber walStart, NpgsqlLogSequenceNumber walEnd, DateTime serverClock, NpgsqlLogSequenceNumber originCommitLsn,
            string originName)
        {
            base.Populate(walStart, walEnd, serverClock);

            OriginCommitLsn = originCommitLsn;
            OriginName = originName;

            return this;
        }
    }
}
