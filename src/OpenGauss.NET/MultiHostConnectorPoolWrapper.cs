using OpenGauss.NET.Internal;
using OpenGauss.NET.Util;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace OpenGauss.NET
{
    sealed class MultiHostConnectorPoolWrapper : ConnectorSource
    {
        internal override bool OwnsConnectors => false;

        readonly MultiHostConnectorPool _wrappedSource;

        public MultiHostConnectorPoolWrapper(OpenGaussConnectionStringBuilder settings, string connString, MultiHostConnectorPool source) : base(settings, connString)
            => _wrappedSource = source;

        internal override (int Total, int Idle, int Busy) Statistics => _wrappedSource.Statistics;

        internal override void Clear() => _wrappedSource.Clear();
        internal override ValueTask<OpenGaussConnector> Get(OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
            => _wrappedSource.Get(conn, timeout, async, cancellationToken);
        internal override bool TryGetIdleConnector([NotNullWhen(true)] out OpenGaussConnector? connector)
            => throw new OpenGaussException("OpenGauss bug: trying to get an idle connector from " + nameof(MultiHostConnectorPoolWrapper));
        internal override ValueTask<OpenGaussConnector?> OpenNewConnector(OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
            => throw new OpenGaussException("OpenGauss bug: trying to open a new connector from " + nameof(MultiHostConnectorPoolWrapper));
        internal override void Return(OpenGaussConnector connector)
            => _wrappedSource.Return(connector);

        internal override void AddPendingEnlistedConnector(OpenGaussConnector connector, Transaction transaction)
            => _wrappedSource.AddPendingEnlistedConnector(connector, transaction);
        internal override bool TryRemovePendingEnlistedConnector(OpenGaussConnector connector, Transaction transaction)
            => _wrappedSource.TryRemovePendingEnlistedConnector(connector, transaction);
        internal override bool TryRentEnlistedPending(Transaction transaction, OpenGaussConnection connection,
            [NotNullWhen(true)] out OpenGaussConnector? connector)
            => _wrappedSource.TryRentEnlistedPending(transaction, connection, out connector);
    }
}
