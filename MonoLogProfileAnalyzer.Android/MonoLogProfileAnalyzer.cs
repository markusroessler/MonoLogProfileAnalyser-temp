using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.Profiler.Log;
using MonoLogProfileAnalyzer.Std;
using DiagDebug = System.Diagnostics.Debug;

namespace MonoLogProfileAnalyzer.Droid
{
    public sealed class MonoLogProfileAnalyzer : IMonoLogProfileAnalyzerRequestReceiver, IDisposable
    {
        private static readonly Lazy<MonoLogProfileAnalyzer> LazyInstance = new Lazy<MonoLogProfileAnalyzer>(
            () => new MonoLogProfileAnalyzer(outputDir: Android.App.Application.Context.GetExternalFilesDir(null).ToString()));
        public static MonoLogProfileAnalyzer Instance => LazyInstance.Value;

        private static readonly string LogCategory = nameof(MonoLogProfileAnalyzer);

        private readonly string _outputDir;

        private uint _maxRequestId = 0;
        private readonly CancellationTokenSource _dispose = new CancellationTokenSource();
        private readonly BlockingCollection<MonoLogProfileAnalyzerRequest> _requestQueue = new BlockingCollection<MonoLogProfileAnalyzerRequest>();


        public MonoLogProfileAnalyzer(string outputDir)
        {
            _outputDir = outputDir;

            var thread = new Thread(() => ProcessAnalyzeRequests())
            {
                Name = nameof(MonoLogProfileAnalyzer)
            };
            thread.Start();
        }


        public void RequestAnalyze()
        {
            var request = new MonoLogProfileAnalyzerRequest(++_maxRequestId, delay: TimeSpan.Zero);
            RequestAnalyze(request);
        }

        void IMonoLogProfileAnalyzerRequestReceiver.RequestAnalyze()
        {
            // short delay to wait for the mlpd to fill
            var request = new MonoLogProfileAnalyzerRequest(++_maxRequestId, delay: TimeSpan.FromSeconds(2));
            RequestAnalyze(request);
        }


        private void RequestAnalyze(MonoLogProfileAnalyzerRequest request)
        {
            _requestQueue.Add(request);
            DiagDebug.WriteLine($"Received request {request}", LogCategory);
        }


        private void ProcessAnalyzeRequests()
        {
            try
            {
                var mlpdFile = $"{_outputDir}/profile.mlpd";
                var visitor = new LogEventVisitorImpl();

                void onEndOfStream() => ExportSamplingResult(visitor);
                using var stream = new BlockingLogStream(new FileStream(mlpdFile, FileMode.Open), _requestQueue, onEndOfStream);
                var logProcessor = new LogProcessor(
                    stream,
                    immediateVisitor: visitor,
                    sortedVisitor: null);
                logProcessor.Process(_dispose.Token);

            }
            catch (Exception ex)
            {
                DiagDebug.WriteLine($"Analyzer failed: {ex}", LogCategory);
            }
        }


        private void ExportSamplingResult(LogEventVisitorImpl visitor)
        {
            var fileName = "profile.txt";
            var outFile = Path.Combine(_outputDir, fileName);
            var sampleFrequency = LogProfiler.SampleFrequency;
            using var writer = new StreamWriter(outFile, append: false);

            DiagDebug.WriteLine($"Writing {fileName} ...", LogCategory);

            foreach (var thread in visitor.ThreadSamplingResults.Values)
            {
                var totalTime = (thread.ManagedCalls?.Values
                    .Select(v => ConvertSampleCountToExecTime(v.SampleCount, sampleFrequency))
                    .Sum()).GetValueOrDefault();

                var blockedTime = (thread.ManagedCalls?.Values
                   .Select(v => ConvertSampleCountToExecTime(v.ThreadBlockedSampleCount, sampleFrequency))
                   .Sum()).GetValueOrDefault();

                var cpuTime = totalTime - blockedTime;

                visitor.ThreadNames.TryGetValue(thread.ThreadId, out var threadName);

                var threadHeader = $"---- Thread {thread.ThreadId} ({threadName}) - {cpuTime}ms CPU + {blockedTime}ms blocked = {totalTime}ms total ----";
                DiagDebug.WriteLine(threadHeader, LogCategory);

                writer.Write($"{threadHeader}\n");

                var topMethods = thread.ManagedCalls?.Values
                    .OrderByDescending(m => m.SampleCount)
                    .Take(20);

                if (topMethods != null)
                {
                    var methodNames = visitor.MethodNames;

                    foreach (var method in topMethods)
                        AppendMethodsRecursively(method, depth: 1, writer, methodNames, sampleFrequency);
                }

                writer.Write("\n\n\n");
            }

            writer.Flush();
            visitor.ThreadSamplingResults.Clear();
        }

        private static void AppendMethodsRecursively(MethodSamplingResult method, int depth, StreamWriter writer, IReadOnlyDictionary<long, string> methodNames, int sampleFrequency)
        {
            var totalTime = ConvertSampleCountToExecTime(method.SampleCount, sampleFrequency);
            if (totalTime > 10)
            {
                var blockedTime = ConvertSampleCountToExecTime(method.ThreadBlockedSampleCount, sampleFrequency);
                var cpuTime = totalTime - blockedTime;
                var blockedPercent = Math.Round(blockedTime / totalTime * 100);
                var blockedSuffix = blockedPercent > 0 ? $" ({blockedPercent}% blocked)" : "";
                var methodName = methodNames[method.MethodPointer];
                writer.Write(new string('\t', depth));
                writer.Write($"{totalTime}ms{blockedSuffix} : {methodName} --- {cpuTime}ms CPU + {blockedTime}ms blocked = {totalTime}ms total ({method.SampleCount} samples)\n");
            }

            var children = method.ManagedCalls?.Values
                .OrderByDescending(m => m.SampleCount);
            if (children == null)
                return;

            foreach (var childMethod in children)
                AppendMethodsRecursively(childMethod, depth + 1, writer, methodNames, sampleFrequency);
        }

        private static double ConvertSampleCountToExecTime(uint sampleCount, int sampleFreq)
        {
            return Math.Round((double)sampleCount / sampleFreq * 1000, digits: 1);
        }


        public void Dispose()
        {
            if (_dispose.IsCancellationRequested)
                return;

            _dispose.Cancel();
            _dispose.Dispose();

            _requestQueue.CompleteAdding();
            _requestQueue.Dispose();
        }
    }
}
