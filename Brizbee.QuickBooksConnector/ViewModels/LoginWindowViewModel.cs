using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.QuickBooksConnector.Messages;
using Hellang.MessageBus;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Brizbee.QuickBooksConnector.ViewModels
{
    public class LoginWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public bool IsEnabled { get; set; }

        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        public async System.Threading.Tasks.Task Login()
        {
            await LoadCredentials();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private async System.Threading.Tasks.Task LoadCredentials()
        {
            IsEnabled = false;
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
            }
            else
            {
                IsEnabled = true;
                OnPropertyChanged("IsEnabled");
                throw new Exception(response.Content);
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
                IsEnabled = false;
                OnPropertyChanged("IsEnabled");

                // Send message to refresh user details
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new SignedInMessage());

                // Send message to refresh commits
                (Application.Current.Properties["MessageBus"] as MessageBus)
                    .Publish(new RefreshCommitsMessage());
            }
            else
            {
                IsEnabled = true;
                OnPropertyChanged("IsEnabled");
                throw new Exception(response.Content);
            }
        }
    }
}
