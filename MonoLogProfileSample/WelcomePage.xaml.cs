using System;
using System.Collections.Generic;
using System.Threading;
using MonoLogProfileAnalyzer.Std;
using Xamarin.Forms;

namespace MonoLogProfileSample
{
    public partial class WelcomePage : ContentPage
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        void ThreadSleep_Clicked(System.Object sender, System.EventArgs e)
        {
            Thread.Sleep(5_000);
            IMonoLogProfileAnalyzerRequestReceiver.Instance.RequestAnalyze();
        }

        async void PushEntriesPage_Clicked(System.Object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new EntriesPage());
        }
    }
}
