using Brizbee.Mobile.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Brizbee.Mobile.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class RegisterPage : ContentPage
	{
		public RegisterPage ()
		{
			InitializeComponent ();
            (BindingContext as RegisterViewModel).Page = this;
        }

        private void PickerCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            if (selectedIndex != -1)
            {
                (BindingContext as RegisterViewModel).RefreshTimeZones();
                //monkeyNameLabel.Text = (string)picker.ItemsSource[selectedIndex];
            }
        }
    }
}