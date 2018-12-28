using Brizbee.Common.Models;
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
            var t = RefreshCurrentPunch();
            t.ContinueWith(task => {
                /* Log issue, deal with it or whatever! */
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async System.Threading.Tasks.Task RefreshCurrentPunch()
        {
            IsBusy = true;

            // Build request
            var request = new RestRequest("odata/Punches/Default.Current?$expand=Task($expand=Job($expand=Customer))", Method.GET);
            
            // Execute request
            var response = await client.ExecuteTaskAsync<Punch>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                if (response.Data != null)
                {
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
                    Since = string.Format("SINCE {0}",
                            response.Data.CreatedAt.ToString("MMM d, yyyy h:mm tt"))
                        .ToUpper();
                    IsPunchedIn = true;
                    OnPropertyChanged(Name = "TaskNumberAndName");
                    OnPropertyChanged(Name = "JobNumberAndName");
                    OnPropertyChanged(Name = "CustomerNumberAndName");
                    OnPropertyChanged(Name = "Since");
                    OnPropertyChanged(Name = "IsPunchedIn");
                    IsBusy = false;
                }
                else
                {
                    IsBusy = false;
                    throw new Exception("There is no current punch for the user");
                }
            }
            else
            {
                IsPunchedOut = true;
                OnPropertyChanged(Name = "IsPunchedOut");
                IsBusy = false;
                throw new Exception(response.Content);
            }
        }
    }
}
