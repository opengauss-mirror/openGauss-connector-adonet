using System;
using System.Threading.Tasks;

namespace OpenGauss.NET
{
    interface ICancelable : IDisposable, IAsyncDisposable
    {
        void Cancel();

        Task CancelAsync();
    }
}
