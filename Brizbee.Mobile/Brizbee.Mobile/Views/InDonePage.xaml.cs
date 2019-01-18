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
	public partial class InDonePage : ContentPage
    {
        public InDonePage()
		{
			InitializeComponent();
            (BindingContext as InDoneViewModel).Page = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}