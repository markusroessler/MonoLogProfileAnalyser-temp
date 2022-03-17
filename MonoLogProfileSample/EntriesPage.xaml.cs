using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace MonoLogProfileSample
{
    public partial class EntriesPage : ContentPage
    {
        public EntriesPage()
        {
            InitializeComponent();
        }

        void AddEntry_Clicked(System.Object sender, System.EventArgs e)
        {
            _layout.Children.Add(_entry);
        }

        void RemoveEntry_Clicked(System.Object sender, System.EventArgs e)
        {
            _layout.Children.Remove(_entry);
        }

        void ToggleEntryVisibility_Clicked(System.Object sender, System.EventArgs e)
        {
            _entry.IsVisible = !_entry.IsVisible;
        }
    }
}
