using Brizbee.Mobile.Views;
using RestSharp;
using System;
using System.Collections.Generic;
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

            // Build request
            var request = new RestRequest("odata/Punches/Default.PunchOut", Method.POST);
            
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
