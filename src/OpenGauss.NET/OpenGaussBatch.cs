using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.Internal;

namespace OpenGauss.NET
{
    /// <inheritdoc />
    public class OpenGaussBatch : DbBatch
    {
        readonly OpenGaussCommand _command;

        /// <inheritdoc />
        protected override DbBatchCommandCollection DbBatchCommands => BatchCommands;

        /// <inheritdoc cref="DbBatch.BatchCommands"/>
        public new OpenGaussBatchCommandCollection BatchCommands { get; }

        /// <inheritdoc />
        public override int Timeout
        {
            get => _command.CommandTimeout;
            set => _command.CommandTimeout = value;
        }

        /// <inheritdoc cref="DbBatch.Connection"/>
        public new OpenGaussConnection? Connection
        {
            get => _command.Connection;
            set => _command.Connection = value;
        }

        /// <inheritdoc />
        protected override DbConnection? DbConnection
        {
            get => Connection;
            set => Connection = (OpenGaussConnection?)value;
        }

        /// <inheritdoc cref="DbBatch.Transaction"/>
        public new OpenGaussTransaction? Transaction
        {
            get => _command.Transaction;
            set => _command.Transaction = value;
        }

        /// <inheritdoc />
        protected override DbTransaction? DbTransaction
        {
            get => Transaction;
            set => Transaction = (OpenGaussTransaction?)value;
        }

        /// <summary>
        /// Marks all of the batch's result columns as either known or unknown.
        /// Unknown results column are requested them from PostgreSQL in text format, and OpenGauss makes no
        /// attempt to parse them. They will be accessible as strings only.
        /// </summary>
        internal bool AllResultTypesAreUnknown
        {
            get => _command.AllResultTypesAreUnknown;
            set => _command.AllResultTypesAreUnknown = value;
        }

        /// <summary>
        /// Initializes a new <see cref="OpenGaussBatch"/>.
        /// </summary>
        /// <param name="connection">A <see cref="OpenGaussConnection"/> that represents the connection to a PostgreSQL server.</param>
        /// <param name="transaction">The <see cref="OpenGaussTransaction"/> in which the <see cref="OpenGaussCommand"/> executes.</param>
        public OpenGaussBatch(OpenGaussConnection? connection = null, OpenGaussTransaction? transaction = null)
        {
            var batchCommands = new List<OpenGaussBatchCommand>(5);
            _command = new(batchCommands);
            BatchCommands = new OpenGaussBatchCommandCollection(batchCommands);

            Connection = connection;
            Transaction = transaction;
        }

        internal OpenGaussBatch(OpenGaussConnector connector)
        {
            var batchCommands = new List<OpenGaussBatchCommand>(5);
            _command = new(connector, batchCommands);
            BatchCommands = new OpenGaussBatchCommandCollection(batchCommands);
        }

        /// <inheritdoc />
        protected override DbBatchCommand CreateDbBatchCommand()
            => new OpenGaussBatchCommand();

        /// <inheritdoc />
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => ExecuteReader(behavior);

        /// <inheritdoc cref="DbBatch.ExecuteReader"/>
        public new OpenGaussDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
            => _command.ExecuteReader();

        /// <inheritdoc />
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
            => await ExecuteReaderAsync(cancellationToken);

        /// <inheritdoc cref="DbBatch.ExecuteReaderAsync(CancellationToken)"/>
        public new Task<OpenGaussDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default)
            => _command.ExecuteReaderAsync(cancellationToken);

        /// <inheritdoc cref="DbBatch.ExecuteReaderAsync(CommandBehavior,CancellationToken)"/>
        public new Task<OpenGaussDataReader> ExecuteReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken = default)
            => _command.ExecuteReaderAsync(behavior, cancellationToken);

        /// <inheritdoc />
        public override int ExecuteNonQuery()
            => _command.ExecuteNonQuery();

        /// <inheritdoc />
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
            => _command.ExecuteNonQueryAsync(cancellationToken);

        /// <inheritdoc />
        public override object? ExecuteScalar()
            => _command.ExecuteScalar();

        /// <inheritdoc />
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
            => _command.ExecuteScalarAsync(cancellationToken);

        /// <inheritdoc />
        public override void Prepare()
            => _command.Prepare();

        /// <inheritdoc />
        public override Task PrepareAsync(CancellationToken cancellationToken = default)
            => _command.PrepareAsync(cancellationToken);

        /// <inheritdoc />
        public override void Cancel() => _command.Cancel();
    }
}
