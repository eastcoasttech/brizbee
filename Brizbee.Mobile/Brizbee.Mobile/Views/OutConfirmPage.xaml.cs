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
	public partial class OutConfirmPage : ContentPage
    {
        INavigation nav = Application.Current.MainPage.Navigation;

        public OutConfirmPage ()
		{
			InitializeComponent ();
		}

        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            nav.PopAsync();
        }

        private void BtnSave_Clicked(object sender, EventArgs e)
        {

        }
    }
}