using System;
namespace MonoLogProfileAnalyzer.Droid
{
    internal readonly struct MonoLogProfileAnalyzerRequest
    {
        internal uint Id { get; }
        internal TimeSpan Delay { get; }


        internal MonoLogProfileAnalyzerRequest(uint id, TimeSpan delay)
        {
            Id = id;
            Delay = delay;
        }


        public override string ToString() => $"[Id: {Id}, Delay: {Delay}]";
    }
}
