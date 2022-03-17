using System;
using Android.App;
using Android.Content;

namespace MonoLogProfileAnalyzer.Droid
{
    /// <summary>
    /// usage: adb shell am broadcast -n com.company.monologprofilesample/monologprofileanalyzer.droid.MonoLogProfileBroadcastReceiver   
    /// </summary>
    [BroadcastReceiver(Name = "monologprofileanalyzer.droid.MonoLogProfileBroadcastReceiver", Enabled = true, Exported = true)]
    public sealed class MonoLogProfileBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            MonoLogProfileAnalyzer.Instance.RequestAnalyze();
        }
    }

}
