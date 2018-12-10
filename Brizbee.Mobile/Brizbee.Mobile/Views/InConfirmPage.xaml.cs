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
	public partial class InConfirmPage : ContentPage
	{
		public InConfirmPage ()
		{
			InitializeComponent ();
		}

        private void BtnCancel_Clicked(object sender, EventArgs e)
        {

        }
    }
}