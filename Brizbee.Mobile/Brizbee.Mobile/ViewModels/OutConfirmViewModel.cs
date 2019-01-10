using Brizbee.Mobile.Views;
using Plugin.DeviceInfo;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class OutConfirmViewModel : BaseViewModel
    {
        public bool IsEnabled { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand PunchOutCommand { get; }

        public OutConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Punch Out";

            PunchOutCommand = new Command(async () => await PunchOut());
        }

        private async System.Threading.Tasks.Task PunchOut()
        {
            IsEnabled = false;
            IsBusy = true;
            string device = "";
            try
            {
                device = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    CrossDeviceInfo.Current.Idiom,
                    CrossDeviceInfo.Current.Platform,
                    CrossDeviceInfo.Current.AppVersion,
                    CrossDeviceInfo.Current.AppBuild,
                    CrossDeviceInfo.Current.DeviceName,
                    CrossDeviceInfo.Current.Manufacturer,
                    CrossDeviceInfo.Current.Version,
                    CrossDeviceInfo.Current.VersionNumber,
                    CrossDeviceInfo.Current.Model);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
            }

            // Build request
            var request = new RestRequest("odata/Punches/Default.PunchOut", Method.POST);
            request.AddJsonBody(new
            {
                SourceForOutAt = device
            });

            // Execute request
            var response = await client.ExecuteTaskAsync(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.Created))
            {
                IsBusy = false;
                var nav = Application.Current.MainPage.Navigation;
                await nav.PushAsync(new OutDonePage());
            }
            else
            {
                IsBusy = false;
                IsEnabled = true;
                throw new Exception(response.Content);
            }
        }
    }
}
