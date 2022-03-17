using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Mono.Profiler.Log;

namespace MonoLogProfileAnalyzer.Droid
{
    internal sealed class LogEventVisitorImpl : LogEventVisitor
    {
        private static readonly string LogCategory = nameof(LogEventVisitorImpl);

        private readonly IDictionary<long, ThreadSamplingResult> _threadSamplingResults = new Dictionary<long, ThreadSamplingResult>();
        internal IDictionary<long, ThreadSamplingResult> ThreadSamplingResults => _threadSamplingResults;

        private readonly IDictionary<long, string> _threadNames = new Dictionary<long, string>();
        internal IReadOnlyDictionary<long, string> ThreadNames => new ReadOnlyDictionary<long, string>(_threadNames);

        private readonly IDictionary<long, string> _methodNames = new Dictionary<long, string>();
        internal IReadOnlyDictionary<long, string> MethodNames => new ReadOnlyDictionary<long, string>(_methodNames);

        private static readonly ISet<string> ThreadBlockingMethodNames = new HashSet<string>
        {
            "System.Threading.Monitor:ObjWait",
            "System.Threading.WaitHandle:Wait",
            "System.Threading.Thread:Sleep",
            "System.Threading.Thread:Join"
        };
        private readonly ISet<long> _threadBlockingMethods = new HashSet<long>();



        public override void Visit(ThreadNameEvent ev)
        {
            base.Visit(ev);

            _threadNames[ev.ThreadId] = ev.Name;
        }


        public override void Visit(JitEvent ev)
        {
            base.Visit(ev);

            var methodPointer = ev.MethodPointer;
            var methodName = ev.Name;

            _methodNames[methodPointer] = methodName;

            if (ThreadBlockingMethodNames.Where(n => methodName.StartsWith(n)).Any())
                _threadBlockingMethods.Add(methodPointer);
        }


        public override void Visit(SampleHitEvent ev)
        {
            base.Visit(ev);

            var sampleTimestamp = ev.Timestamp;

            var threadId = ev.ThreadId;
            if (!_threadSamplingResults.TryGetValue(threadId, out var currentThread))
            {
                currentThread = new ThreadSamplingResult { ThreadId = threadId };
                _threadSamplingResults[threadId] = currentThread;
            }

            var managedTrace = ev.ManagedBacktrace;
            if (!managedTrace.Any())
                return;

            var currentManagedCalls = currentThread.ManagedCalls ??= new Dictionary<long, MethodSamplingResult>();
            var threadIsBlocked = managedTrace.Any(m => _threadBlockingMethods.Contains(m));

            for (int i = 0; i < managedTrace.Count; i++)
            {
                var stackItem = managedTrace[i];

                if (!currentManagedCalls.TryGetValue(stackItem, out var currentMethod))
                {
                    currentMethod = new MethodSamplingResult { MethodPointer = stackItem };
                    currentManagedCalls[stackItem] = currentMethod;
                }

                currentMethod.SampleCount++;

                if (threadIsBlocked)
                    currentMethod.ThreadBlockedSampleCount++;

                if (i < managedTrace.Count - 1)
                    currentManagedCalls = currentMethod.ManagedCalls ??= new Dictionary<long, MethodSamplingResult>();
            }
        }
    }


    internal sealed class ThreadSamplingResult
    {
        public long ThreadId { get; set; }
        public IDictionary<long, MethodSamplingResult> ManagedCalls { get; set; }
    }


    internal sealed class MethodSamplingResult
    {
        public long MethodPointer { get; set; }
        public uint SampleCount { get; set; }
        public uint ThreadBlockedSampleCount { get; set; }
        public IDictionary<long, MethodSamplingResult> ManagedCalls { get; set; }
    }
}
