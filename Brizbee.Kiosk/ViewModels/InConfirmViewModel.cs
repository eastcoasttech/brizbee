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
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class InConfirmViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public bool IsEnabled { get; set; }
        public bool ShowTimeZones { get; set; }
        public string JobNumberAndName { get; set; }
        public string CustomerNumberAndName { get; set; }
        public string TaskNumberAndName { get; set; }
        public string TimeZone { get; set; }
        public List<IanaTimeZone> TimeZones { get; set; }
        public List<Country> Countries { get; set; }
        public Country SelectedCountry { get; set; }
        public IanaTimeZone SelectedTimeZone { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand PunchInCommand { get; }

        public InConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Punch In";
            ShowTimeZones = true;

            PunchInCommand = new Command(async () => await PunchIn());

            var user = Application.Current.Properties["CurrentUser"] as User;
            var task = Application.Current.Properties["SelectedTask"] as Task;
            JobNumberAndName = string.Format("{0} - {1}", task.Job.Number, task.Job.Name);
            CustomerNumberAndName = string.Format("{0} - {1}", task.Job.Customer.Number, task.Job.Customer.Name);
            TaskNumberAndName = string.Format("{0} - {1}", task.Number, task.Name);
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

        private async System.Threading.Tasks.Task PunchIn()
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

            //try
            //{
            //    var locationRequest = new GeolocationRequest(GeolocationAccuracy.Medium);
            //    var location = await Geolocation.GetLocationAsync(locationRequest);

            //    if (location != null)
            //    {
            //        Trace.TraceInformation($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
            //    }
            //    else
            //    {
            //        Trace.TraceWarning("Could not get location");
            //    }
            //}
            //catch (FeatureNotSupportedException fnsEx)
            //{
            //    // Handle not supported on device exception
            //    Trace.TraceWarning(fnsEx.ToString());
            //}
            //catch (FeatureNotEnabledException fneEx)
            //{
            //    // Handle not enabled on device exception
            //    Trace.TraceWarning(fneEx.ToString());
            //}
            //catch (PermissionException pEx)
            //{
            //    // Handle permission exception
            //    Trace.TraceWarning(pEx.ToString());
            //}
            //catch (Exception ex)
            //{
            //    // Unable to get location
            //    Trace.TraceWarning(ex.ToString());
            //}

            // Build request
            var request = new RestRequest("odata/Punches/Default.PunchIn", Method.POST);
            request.AddJsonBody(new
            {
                TaskId = (Application.Current.Properties["SelectedTask"] as Task).Id,
                SourceForInAt = device,
                InAtTimeZone = (Application.Current.Properties["CurrentUser"] as User).TimeZone
            });

            // Execute request
            var response = await client.ExecuteTaskAsync(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.Created))
            {
                IsBusy = false;
                var nav = Application.Current.MainPage.Navigation;
                await nav.PushAsync(new InDonePage());
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
