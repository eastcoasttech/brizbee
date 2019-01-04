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
	public partial class StatusPage : ContentPage
    {
        INavigation nav = Application.Current.MainPage.Navigation;

        public StatusPage()
		{
			InitializeComponent();
		}

        private void BtnExit_Clicked(object sender, EventArgs e)
        {
            nav.PopToRootAsync();
        }

        private void BtnPunchIn_Clicked(object sender, EventArgs e)
        {
            nav.PushAsync(new InTaskPage());
        }

        private void BtnPunchOut_Clicked(object sender, EventArgs e)
        {
            nav.PushAsync(new OutConfirmPage());
        }
    }
}