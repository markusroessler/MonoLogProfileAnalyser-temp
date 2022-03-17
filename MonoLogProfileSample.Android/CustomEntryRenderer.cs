using System;
using Android.Content;
using MonoLogProfileSample.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Entry), typeof(CustomEntryRenderer))]
namespace MonoLogProfileSample.Droid
{
    public class CustomEntryRenderer : EntryRenderer
    {
        public CustomEntryRenderer(Context context) : base(context)
        {
        }

        protected override FormsEditText CreateNativeControl()
        {
            return base.CreateNativeControl();
        }
    }
}
