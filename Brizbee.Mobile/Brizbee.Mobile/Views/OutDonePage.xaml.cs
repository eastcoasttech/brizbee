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
	public partial class OutDonePage : ContentPage
    {
        INavigation nav = Application.Current.MainPage.Navigation;

        public OutDonePage()
		{
			InitializeComponent();
		}

        private void BtnExit_Clicked(object sender, EventArgs e)
        {
            nav.PopAsync();
        }

        private void BtnMore_Clicked(object sender, EventArgs e)
        {
            nav.PopAsync();
        }
    }
}