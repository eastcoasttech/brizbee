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
        public string TimeZone { get; set; }
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
            var response = await client.ExecuteTaskAsync<ODataResponse<Punch>>(request);
            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {

                    if (response.Data.Value.Count != 0)
                    {
                        var punch = response.Data.Value[0];

                        var inAt = DateTime.SpecifyKind(punch.InAt, DateTimeKind.Local);

                        // Get abbreviation for time zone of InAt
                        var tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(punch.InAtTimeZone);
                        var nowInstant = SystemClock.Instance.GetCurrentInstant();
                        var nowLocal = nowInstant.InZone(tz);

                        CustomerNumberAndName = string.Format("{0} - {1}",
                                punch.Task.Job.Customer.Number,
                                punch.Task.Job.Customer.Name)
                            .ToUpper();
                        JobNumberAndName = string.Format("{0} - {1}",
                                punch.Task.Job.Number,
                                punch.Task.Job.Name)
                            .ToUpper();
                        TaskNumberAndName = string.Format("{0} - {1}",
                                punch.Task.Number,
                                punch.Task.Name)
                            .ToUpper();
                        Since = string.Format("SINCE {0}",
                                inAt.ToString("MMM d, yyyy h:mm tt"))
                            .ToUpper();
                        TimeZone = punch.InAtTimeZone.ToUpper();
                        IsPunchedOut = false;
                        IsPunchedIn = true;
                        OnPropertyChanged("TaskNumberAndName");
                        OnPropertyChanged("JobNumberAndName");
                        OnPropertyChanged("CustomerNumberAndName");
                        OnPropertyChanged("Since");
                        OnPropertyChanged("TimeZone");
                        OnPropertyChanged("IsPunchedOut");
                        OnPropertyChanged("IsPunchedIn");
                        IsBusy = false;
                    }
                    else
                    {
                        IsPunchedIn = false;
                        IsPunchedOut = true;
                        OnPropertyChanged("IsPunchedIn");
                        OnPropertyChanged("IsPunchedOut");
                        IsBusy = false;
                    }
                }
                else
                {
                    IsPunchedIn = false;
                    IsPunchedOut = true;
                    OnPropertyChanged("IsPunchedIn");
                    OnPropertyChanged("IsPunchedOut");
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
