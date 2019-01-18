using Brizbee.Common.Models;
using Brizbee.Mobile.Views;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class InTaskViewModel : BaseViewModel
    {
        public Page Page { get; set; }
        public string TaskNumber { get; set; }
        public bool IsEnabled { get; set; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand ContinueCommand { get; }

        public InTaskViewModel()
        {
            IsEnabled = true;
            Title = "Enter Task Number";

            ResetPage();

            ContinueCommand = new Command(async () => await LoadTask());
        }

        public void ResetPage()
        {
            TaskNumber = "";
            OnPropertyChanged("TaskNumber");
            IsBusy = false;
        }

        private async System.Threading.Tasks.Task LoadTask()
        {
            IsEnabled = false;

            // Build request
            var request = new RestRequest("odata/Tasks?$expand=Job($expand=Customer)&$filter=Number eq '" + TaskNumber + "'", Method.GET);

            // Execute request
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
                    await Page.DisplayAlert("Oops!", "There is no task with that number", "Try again");
                }
            }
            else
            {
                IsEnabled = true;
                await Page.DisplayAlert("Oops!", response.Content, "Try again");
            }
        }
    }
}
