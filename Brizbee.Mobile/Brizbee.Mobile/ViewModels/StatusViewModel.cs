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
        public string CurrentCustomer { get; set; }
        public string CurrentJob { get; set; }
        public string CurrentTask { get; set; }
        public string Since { get; set; }
        public bool LoadingCurrentPunch { get; set; }
        public string Name { get; set; }
        public bool IsPunchedIn { get; set; }
        public bool IsPunchedOut { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public StatusViewModel()
        {
            var user = Application.Current.Properties["CurrentUser"] as User;
            Title = "Status";
            Name = user.Name;
            IsPunchedIn = false;
            IsPunchedOut = false;
            var t = RefreshCurrentPunch();
            t.ContinueWith(task => {
                /* Log issue, deal with it or whatever! */
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async System.Threading.Tasks.Task RefreshCurrentPunch()
        {
            LoadingCurrentPunch = true;

            // Build request
            var request = new RestRequest("odata/Punches/Default.Current?$expand=Task($expand=Job($expand=Customer))", Method.GET);
            
            // Execute request
            var response = await client.ExecuteTaskAsync<Punch>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                if (response.Data != null)
                {
                    CurrentCustomer = string.Format("{0} - {1}",
                            response.Data.Task.Job.Customer.Number,
                            response.Data.Task.Job.Customer.Name)
                        .ToUpper();
                    CurrentJob = string.Format("{0} - {1}",
                            response.Data.Task.Job.Number,
                            response.Data.Task.Job.Name)
                        .ToUpper();
                    CurrentTask = string.Format("{0} - {1}",
                            response.Data.Task.Number,
                            response.Data.Task.Name)
                        .ToUpper();
                    Since = string.Format("SINCE {0}",
                            response.Data.CreatedAt.ToString("MMM d, yyyy h:mm tt"))
                        .ToUpper();
                    IsPunchedIn = true;
                    OnPropertyChanged(Name = "CurrentTask");
                    OnPropertyChanged(Name = "CurrentJob");
                    OnPropertyChanged(Name = "CurrentCustomer");
                    OnPropertyChanged(Name = "Since");
                    OnPropertyChanged(Name = "IsPunchedIn");
                }
                else
                {
                    throw new Exception("There is no current punch for the user");
                }
            }
            else
            {
                LoadingCurrentPunch = true;
                throw new Exception(response.Content);
            }
        }
    }
}
