using Brizbee.Common.Models;
using NodaTime;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class StatusViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public string CustomerNumberAndName { get; set; }
        public string JobNumberAndName { get; set; }
        public string TaskNumberAndName { get; set; }
        public string Since { get; set; }
        public string Name { get; set; }
        public bool IsPunchedIn { get; set; }
        public bool IsPunchedOut { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public StatusViewModel()
        {
            var user = Application.Current.Properties["CurrentUser"] as User;
            Title = "Status";
            Name = user.Name.ToUpper();
            IsPunchedIn = false;
            IsPunchedOut = false;
        }

        public async void RefreshCurrentPunch()
        {
            var user = Application.Current.Properties["CurrentUser"] as User;
            var organization = user.Organization;
            IsBusy = true;

            // Build request
            var request = new RestRequest("odata/Punches/Default.Current?$expand=Task($expand=Job($expand=Customer))", Method.GET);

            // Execute request
            var response = await client.ExecuteTaskAsync<Punch>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var inAt = DateTime.SpecifyKind(response.Data.InAt, DateTimeKind.Local);

                    // Get abbreviation for time zone of InAt
                    var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(response.Data.InAtTimeZone);
                    var nowInstant = SystemClock.Instance.GetCurrentInstant();
                    var nowLocal = nowInstant.InZone(tz);

                    CustomerNumberAndName = string.Format("{0} - {1}",
                            response.Data.Task.Job.Customer.Number,
                            response.Data.Task.Job.Customer.Name)
                        .ToUpper();
                    JobNumberAndName = string.Format("{0} - {1}",
                            response.Data.Task.Job.Number,
                            response.Data.Task.Job.Name)
                        .ToUpper();
                    TaskNumberAndName = string.Format("{0} - {1}",
                            response.Data.Task.Number,
                            response.Data.Task.Name)
                        .ToUpper();
                    Since = string.Format("SINCE {0} {1}",
                            inAt.ToString("MMM d, yyyy h:mm tt"),
                            nowLocal.ToDateTimeUnspecified().ToString("z"))
                        .ToUpper();
                    IsPunchedOut = false;
                    IsPunchedIn = true;
                    OnPropertyChanged(Name = "TaskNumberAndName");
                    OnPropertyChanged(Name = "JobNumberAndName");
                    OnPropertyChanged(Name = "CustomerNumberAndName");
                    OnPropertyChanged(Name = "Since");
                    OnPropertyChanged(Name = "IsPunchedOut");
                    OnPropertyChanged(Name = "IsPunchedIn");
                    IsBusy = false;
                }
                else
                {
                    IsPunchedIn = false;
                    IsPunchedOut = true;
                    OnPropertyChanged(Name = "IsPunchedIn");
                    OnPropertyChanged(Name = "IsPunchedOut");
                    IsBusy = false;
                }
            }
            else
            {
                IsBusy = false;
                await Page.DisplayAlert("Oops!", "Could not load the current punch for the user", "Try again");
            }
        }
    }
}
