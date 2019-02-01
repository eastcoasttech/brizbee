using Brizbee.Common.Models;
using Brizbee.Common.Serialization;
using Brizbee.Mobile.Models;
using Brizbee.Mobile.Services;
using Brizbee.Mobile.Views;
using Plugin.DeviceInfo;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class OutConfirmViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public bool IsEnabled { get; set; }
        public bool ShowTimeZones { get; set; }
        public string TimeZone { get; set; }
        public List<IanaTimeZone> TimeZones { get; set; }
        public List<Country> Countries { get; set; }
        public Country SelectedCountry { get; set; }
        public IanaTimeZone SelectedTimeZone { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand PunchOutCommand { get; }

        public OutConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Punch Out";
            ShowTimeZones = true;

            PunchOutCommand = new Command(async () => await PunchOut());

            var user = Application.Current.Properties["CurrentUser"] as User;
            TimeZone = user.TimeZone;

            try
            {
                TimeZones = TimeZoneService.GetTimeZones("");
                SelectedTimeZone = TimeZones.Where(t => t.Id == user.TimeZone).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.ToString());
            }
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
                SourceForOutAt = device,
                OutAtTimeZone = (Application.Current.Properties["CurrentUser"] as User).TimeZone
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
                await Page.DisplayAlert("Oops!", response.Content, "Try again");
            }
        }
    }
}
