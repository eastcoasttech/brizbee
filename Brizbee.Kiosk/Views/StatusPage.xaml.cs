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
	public partial class StatusPage : ContentPage
    {
        INavigation nav = Application.Current.MainPage.Navigation;

        public StatusPage()
		{
			InitializeComponent();
            (BindingContext as StatusViewModel).Page = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Refresh every 15 seconds
            Device.StartTimer(TimeSpan.FromSeconds(15), () => {
                // Run on the main thread so that the interface is updated
                Device.BeginInvokeOnMainThread(() =>
                    (BindingContext as StatusViewModel).RefreshCurrentPunch()
                );
                return true;
            });

            // Refresh now
            (BindingContext as StatusViewModel).RefreshCurrentPunch();
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