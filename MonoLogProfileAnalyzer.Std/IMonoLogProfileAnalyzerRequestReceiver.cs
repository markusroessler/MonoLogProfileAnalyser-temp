using System;

namespace MonoLogProfileAnalyzer.Std
{
    public interface IMonoLogProfileAnalyzerRequestReceiver
    {
        public static IMonoLogProfileAnalyzerRequestReceiver Instance { get; set; }

        public void RequestAnalyze();
    }
}
