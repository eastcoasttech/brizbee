using Brizbee.Mobile.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace Brizbee.Mobile.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class InTaskPage : ContentPage
    {
        INavigation nav = Application.Current.MainPage.Navigation;

        public InTaskPage()
		{
			InitializeComponent();
            (BindingContext as InTaskViewModel).Page = this;
        }

        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            nav.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            (BindingContext as InTaskViewModel).ResetPage();

            txtTaskNumber.Focus();
        }

        private void BtnScan_Clicked(object sender, EventArgs e)
        {
            var scanPage = new ZXingScannerPage();
            // Navigate to our scanner page
            var nav = Application.Current.MainPage.Navigation;
            nav.PushAsync(scanPage);

            scanPage.OnScanResult += (result) =>
            {
                // Stop scanning
                scanPage.IsScanning = false;

                // Pop the page and show the result
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await nav.PopAsync();
                    txtTaskNumber.Text = result.Text;
                    (BindingContext as InTaskViewModel).ContinueCommand.Execute(null);
                    //await DisplayAlert("Scanned Barcode", result.Text, "OK");
                });
            };

            // Options
            var options = new MobileBarcodeScanningOptions
            {
                AutoRotate = false,
                UseFrontCameraIfAvailable = true,
                TryHarder = true,
                PossibleFormats = new List<ZXing.BarcodeFormat>
                {
                   ZXing.BarcodeFormat.CODABAR
                }
            };

            // Add options and customize page
            scanPage = new ZXingScannerPage(options)
            {
                DefaultOverlayTopText = "Align the barcode within the frame",
                DefaultOverlayBottomText = string.Empty,
                DefaultOverlayShowFlashButton = true
            };
        }
    }
}