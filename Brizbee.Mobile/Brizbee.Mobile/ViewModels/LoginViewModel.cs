﻿using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Mobile.Views;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace Brizbee.Mobile.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public string OrganizationCode { get; set; }
        public string PinNumber { get; set; }
        public bool IsEnabled
        {
            get { return !IsBusy; }
        }

        private RestClient client = Application.Current.Properties["RestClient"] as RestClient;

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            Title = "Login to BRIZBEE";

            ResetPage();

            LoginCommand = new Command(async () => await LoadCredentials());
        }

        public void ResetPage()
        {
            OrganizationCode = "";
            PinNumber = "";
            OnPropertyChanged("OrganizationCode");
            OnPropertyChanged("PinNumber");
            IsBusy = false;
            OnPropertyChanged("IsEnabled");
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
                    Method = "pin",
                    PinOrganizationCode = OrganizationCode,
                    PinUserPin = PinNumber
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
                Trace.TraceWarning(response.Content);
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
                Trace.TraceWarning(response.Content);
            }
        }
    }
}