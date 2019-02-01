using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Common.Serialization;
using Brizbee.Mobile.Models;
using Brizbee.Mobile.Services;
using Brizbee.Mobile.Views;
using NodaTime;
using NodaTime.TimeZones;
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
    public class RegisterViewModel : BaseViewModel
    {
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
        public string OrganizationName { get; set; }
        public Page Page { get; set; }
        public string Password { get; set; }
        public List<IanaTimeZone> TimeZones { get; set; }
        public List<Country> Countries { get; set; }
        public Country SelectedCountry { get; set; }
        public IanaTimeZone SelectedTimeZone { get; set; }
        public bool IsEnabled
        {
            get { return !IsBusy; }
        }
        public ICommand SaveCommand { get; }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public RegisterViewModel()
        {
            Title = "Sign Up for BRIZBEE";
            RefreshCountries();

            SaveCommand = new Command(async () => await Register());
        }

        public void RefreshCountries()
        {
            // Populate the list of countries
            Countries = TimeZoneService.GetCountries();
            OnPropertyChanged("Countries");
            SelectedCountry = Countries.Where(c => c.Name == "United States").FirstOrDefault();
            OnPropertyChanged("SelectedCountry");
        }

        public void RefreshTimeZones()
        {
            // Populate the list of IANA time zones
            TimeZones = TimeZoneService.GetTimeZones(SelectedCountry.CountryCode);
            OnPropertyChanged("TimeZones");
            SelectedTimeZone = TimeZones.FirstOrDefault();
            OnPropertyChanged("SelectedTimeZone");
        }

        private async System.Threading.Tasks.Task LoadCredentials()
        {
            IsBusy = true;
            OnPropertyChanged("IsEnabled");

            // Build request to authenticate user
            var request = new RestRequest("odata/Users/Default.Authenticate", Method.POST);
            request.AddJsonBody(new
            {
                Session = new
                {
                    Method = "email",
                    EmailAddress = EmailAddress,
                    EmailPassword = Password
                }
            });

            // Execute request to authenticate user
            var response = await client.ExecuteTaskAsync<Credential>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.Created))
            {
                // Save the authentication credentials for later
                Application.Current.Properties["AuthUserId"] = response.Data.AuthUserId;
                Application.Current.Properties["AuthExpiration"] = response.Data.AuthExpiration;
                Application.Current.Properties["AuthToken"] = response.Data.AuthToken;

                // Add the client headers for authentication
                client.AddDefaultHeader("AUTH_USER_ID", response.Data.AuthUserId);
                client.AddDefaultHeader("AUTH_EXPIRATION", response.Data.AuthExpiration);
                client.AddDefaultHeader("AUTH_TOKEN", response.Data.AuthToken);

                await LoadUser();

                var nav = Application.Current.MainPage.Navigation;
                await nav.PushAsync(new StatusPage());
            }
            else
            {
                IsBusy = false;
                OnPropertyChanged("IsEnabled");
                await Page.DisplayAlert("Oops!", response.Content, "Try again");
            }
        }

        private async System.Threading.Tasks.Task LoadUser()
        {
            // Build request to retrieve authenticated user
            var request = new RestRequest(string.Format("odata/Users({0})?$expand=Organization",
                Application.Current.Properties["AuthUserId"]), Method.GET);

            // Execute request to retrieve authenticated user
            var response = await client.ExecuteTaskAsync<User>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                // Save the authenticated user for later
                Application.Current.Properties["CurrentUser"] = response.Data;

                // Send message to refresh user details
                //(Application.Current.Properties["MessageBus"] as MessageBus)
                //    .Publish(new SignedInMessage());

                // Send message to refresh commits
                //(Application.Current.Properties["MessageBus"] as MessageBus)
                //    .Publish(new RefreshCommitsMessage());
            }
            else
            {
                IsBusy = false;
                OnPropertyChanged("IsEnabled");
                await Page.DisplayAlert("Oops!", response.Content, "Try again");
            }
        }

        private async System.Threading.Tasks.Task Register()
        {
            IsBusy = true;
            OnPropertyChanged("IsEnabled");

            // Build request to create user and organization
            var request = new RestRequest("odata/Users/Default.Register", Method.POST);
            request.AddJsonBody(new
            {
                Organization = new
                {
                    Name = OrganizationName
                },
                User = new {
                    FullName = FullName,
                    EmailAddress = EmailAddress,
                    Password = Password,
                    TimeZone = SelectedTimeZone.Id
                }
            });

            // Execute request to authenticate user
            var response = await client.ExecuteTaskAsync(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.Created))
            {
                // Do something
            }
            else
            {
                IsBusy = false;
                OnPropertyChanged("IsEnabled");
                await Page.DisplayAlert("Oops!", response.Content, "Try again");
            }
        }
    }
}
