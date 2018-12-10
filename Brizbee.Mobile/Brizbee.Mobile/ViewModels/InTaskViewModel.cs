using Brizbee.Common.Models;
using Brizbee.Mobile.Views;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class InTaskViewModel : BaseViewModel
    {
        public string TaskNumber { get; set; }
        public bool IsEnabled { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand ContinueCommand { get; }

        public InTaskViewModel()
        {
            IsEnabled = true;
            Title = "Enter Task Number";

            ContinueCommand = new Command(async () => await LoadTask());
        }

        private async System.Threading.Tasks.Task LoadTask()
        {
            IsEnabled = false;

            // Build request to get task
            var request = new RestRequest("odata/Tasks?$expand=Job($expand=Customer)&$filter=Number eq '" + TaskNumber + "'", Method.GET);

            // Execute request to authenticate user
            var response = await client.ExecuteTaskAsync<ODataResponse<Task>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                if (response.Data.Value.Count != 0)
                {
                    Application.Current.Properties["SelectedTask"] = response.Data.Value[0];
                    var nav = Application.Current.MainPage.Navigation;
                    await nav.PopAsync();
                    await nav.PushAsync(new InConfirmPage());
                }
                else
                {
                    throw new Exception("There is no task with that number");
                }
            }
            else
            {
                IsEnabled = true;
                throw new Exception(response.Content);
            }
        }
    }
}
