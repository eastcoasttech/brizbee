using Brizbee.Common.Models;
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
    public class InConfirmViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public bool IsEnabled { get; set; }
        public string JobNumberAndName { get; set; }
        public string CustomerNumberAndName { get; set; }
        public string TaskNumberAndName { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand PunchInCommand { get; }

        public InConfirmViewModel()
        {
            IsEnabled = true;
            Title = "Confirm Punch In";

            PunchInCommand = new Command(async () => await PunchIn());

            var task = Application.Current.Properties["SelectedTask"] as Task;
            JobNumberAndName = string.Format("{0} - {1}", task.Job.Number, task.Job.Name);
            CustomerNumberAndName = string.Format("{0} - {1}", task.Job.Customer.Number, task.Job.Customer.Name);
            TaskNumberAndName = string.Format("{0} - {1}", task.Number, task.Name);
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

            // Build request
            var request = new RestRequest("odata/Punches/Default.PunchIn", Method.POST);
            request.AddJsonBody(new
            {
                TaskId = (Application.Current.Properties["SelectedTask"] as Task).Id,
                SourceForInAt = device
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
