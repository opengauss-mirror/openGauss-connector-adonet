using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Util;

namespace OpenGauss.NET
{
    sealed class UnpooledConnectorSource : ConnectorSource
    {
        public UnpooledConnectorSource(OpenGaussConnectionStringBuilder settings, string connString)
            : base(settings, connString)
        {
        }

        volatile int _numConnectors;

        internal override (int Total, int Idle, int Busy) Statistics => (_numConnectors, 0, _numConnectors);

        internal override bool OwnsConnectors => true;

        internal override async ValueTask<OpenGaussConnector> Get(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
        {
            var connector = new OpenGaussConnector(this, conn);
            await connector.Open(timeout, async, cancellationToken);
            Interlocked.Increment(ref _numConnectors);
            return connector;
        }

        internal override bool TryGetIdleConnector([NotNullWhen(true)] out OpenGaussConnector? connector)
        {
            connector = null;
            return false;
        }

        internal override ValueTask<OpenGaussConnector?> OpenNewConnector(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken)
            => new((OpenGaussConnector?)null);

        internal override void Return(OpenGaussConnector connector)
        {
            Interlocked.Decrement(ref _numConnectors);
            connector.Close();
        }

        internal override void Clear() {}

        internal override bool TryRentEnlistedPending(Transaction transaction, OpenGaussConnection connection,
            [NotNullWhen(true)] out OpenGaussConnector? connector)
        {
            connector = null;
            return false;
        }

        internal override bool TryRemovePendingEnlistedConnector(OpenGaussConnector connector, Transaction transaction) => false;
    }
}
