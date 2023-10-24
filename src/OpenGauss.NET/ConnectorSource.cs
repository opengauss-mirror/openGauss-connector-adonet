using OpenGauss.NET.Internal;
using OpenGauss.NET.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace OpenGauss.NET
{
    abstract class ConnectorSource : IDisposable
    {
        internal OpenGaussConnectionStringBuilder Settings { get; }

        /// <summary>
        /// Contains the connection string returned to the user from <see cref="OpenGaussConnection.ConnectionString"/>
        /// after the connection has been opened. Does not contain the password unless Persist Security Info=true.
        /// </summary>
        internal string UserFacingConnectionString { get; }

        // Note that while the dictionary is protected by locking, we assume that the lists it contains don't need to be
        // (i.e. access to connectors of a specific transaction won't be concurrent)
        protected readonly Dictionary<Transaction, List<OpenGaussConnector>> _pendingEnlistedConnectors
            = new();

        internal abstract (int Total, int Idle, int Busy) Statistics { get; }

        internal ConnectorSource(OpenGaussConnectionStringBuilder settings, string connString)
        {
            Settings = settings;

            UserFacingConnectionString = settings.PersistSecurityInfo
                ? connString
                : settings.ToStringWithoutPassword();
        }

        internal abstract ValueTask<OpenGaussConnector> Get(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken);

        internal abstract bool TryGetIdleConnector([NotNullWhen(true)] out OpenGaussConnector? connector);

        internal abstract ValueTask<OpenGaussConnector?> OpenNewConnector(
            OpenGaussConnection conn, OpenGaussTimeout timeout, bool async, CancellationToken cancellationToken);

        internal abstract void Return(OpenGaussConnector connector);

        internal abstract void Clear();

        internal abstract bool OwnsConnectors { get; }

        public virtual void Dispose() { }

        #region Pending Enlisted Connections

        internal virtual void AddPendingEnlistedConnector(OpenGaussConnector connector, Transaction transaction)
        {
            lock (_pendingEnlistedConnectors)
            {
                if (!_pendingEnlistedConnectors.TryGetValue(transaction, out var list))
                    list = _pendingEnlistedConnectors[transaction] = new List<OpenGaussConnector>();
                list.Add(connector);
            }
        }

        internal virtual bool TryRemovePendingEnlistedConnector(OpenGaussConnector connector, Transaction transaction)
        {
            lock (_pendingEnlistedConnectors)
            {
                if (!_pendingEnlistedConnectors.TryGetValue(transaction, out var list))
                    return false;
                list.Remove(connector);
                if (list.Count == 0)
                    _pendingEnlistedConnectors.Remove(transaction);
                return true;
            }
        }

        internal virtual bool TryRentEnlistedPending(Transaction transaction, OpenGaussConnection connection,
            [NotNullWhen(true)] out OpenGaussConnector? connector)
        {
            lock (_pendingEnlistedConnectors)
            {
                if (!_pendingEnlistedConnectors.TryGetValue(transaction, out var list))
                {
                    connector = null;
                    return false;
                }
                connector = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                if (list.Count == 0)
                    _pendingEnlistedConnectors.Remove(transaction);
                return true;
            }
        }

        #endregion
    }
}
