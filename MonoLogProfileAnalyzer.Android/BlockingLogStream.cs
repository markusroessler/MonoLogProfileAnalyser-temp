using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Mono.Profiler.Log;
using DiagDebug = System.Diagnostics.Debug;

namespace MonoLogProfileAnalyzer.Droid
{
    internal sealed class BlockingLogStream : LogStream
    {
        private static readonly string LogCategory = nameof(MonoLogProfileAnalyzer);

        private readonly Action _onEndOfStream;
        private readonly BlockingCollection<MonoLogProfileAnalyzerRequest> _requestQueue;
        private MonoLogProfileAnalyzerRequest? _currentRequest;


        public BlockingLogStream(Stream baseStream, BlockingCollection<MonoLogProfileAnalyzerRequest> requestQueue, Action onEndOfStream) : base(baseStream)
        {
            _requestQueue = requestQueue;
            _onEndOfStream = onEndOfStream;
        }


        public override int ReadByte()
        {
            TakeFirstRequest();
            return base.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            TakeFirstRequest();
            return base.Read(buffer, offset, count);
        }


        public override bool EndOfStream
        {
            get
            {
                while (base.EndOfStream)
                {
                    _onEndOfStream();
                    DiagDebug.WriteLine($"Request {_currentRequest} processed", LogCategory);

                    TakeNextRequest();
                }

                return false;
            }
        }


        private void TakeNextRequest()
        {
            _currentRequest = _requestQueue.Take();
            DiagDebug.WriteLine($"Processing request {_currentRequest} ...", LogCategory);
            Thread.Sleep(_currentRequest.Value.Delay);
        }


        private void TakeFirstRequest()
        {
            if (_currentRequest == null)
                TakeNextRequest();
        }
    }
}
