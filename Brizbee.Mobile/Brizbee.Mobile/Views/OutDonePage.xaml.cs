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
	public partial class OutDonePage : ContentPage
    {
        public OutDonePage()
		{
			InitializeComponent();
            (BindingContext as OutDoneViewModel).Page = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}