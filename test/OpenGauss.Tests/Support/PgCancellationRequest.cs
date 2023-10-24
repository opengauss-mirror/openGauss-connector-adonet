using System.IO;
using OpenGauss.NET.Internal;

namespace OpenGauss.Tests.Support
{
    class PgCancellationRequest
    {
        readonly OpenGaussReadBuffer _readBuffer;
        readonly OpenGaussWriteBuffer _writeBuffer;
        readonly Stream _stream;

        public int ProcessId { get; }
        public int Secret { get; }

        bool completed;

        public PgCancellationRequest(OpenGaussReadBuffer readBuffer, OpenGaussWriteBuffer writeBuffer, Stream stream, int processId, int secret)
        {
            _readBuffer = readBuffer;
            _writeBuffer = writeBuffer;
            _stream = stream;

            ProcessId = processId;
            Secret = secret;
        }

        public void Complete()
        {
            if (completed)
                return;

            _readBuffer.Dispose();
            _writeBuffer.Dispose();
            _stream.Dispose();

            completed = true;
        }
    }
}
