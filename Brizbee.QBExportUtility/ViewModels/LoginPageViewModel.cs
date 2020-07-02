using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.QBExportUtility.Exceptions;
using Hellang.MessageBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Brizbee.QBExportUtility.ViewModels
{
    public class LoginPageViewModel : INotifyPropertyChanged
    {
        public string EmailAddress { get; set; }
        public bool IsEnabled { get; set; }
        public string Password { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private RestClient client;

        public async System.Threading.Tasks.Task Login()
        {
            // Reintialize the HTTP client
            client = new RestClient("https://brizbee.gowitheast.com/");
            Application.Current.Properties["Client"] = client;

            await LoadCredentials();
        }

        public async System.Threading.Tasks.Task<string> CheckLatestVersion()
        {
            // Build request to check latest version of software
            var versionClient = new RestClient("https://ects1.blob.core.windows.net/");
            var versionRequest = new RestRequest("brizbee-public/latest.json", Method.GET);

            // Execute request to check the latest version
            var versionResponse = await versionClient.ExecuteTaskAsync(versionRequest);
            if ((versionResponse.ResponseStatus == ResponseStatus.Completed) &&
                    (versionResponse.StatusCode == System.Net.HttpStatusCode.OK))
            {
                var versionResult = JsonConvert.DeserializeObject<JObject>(versionResponse.Content); // Deserialize manually
                return versionResult["QuickBooksDesktopExportUtility"].ToString();
            }
            else
            {
                Trace.TraceWarning(versionResponse.Content);
                return "";
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

                if (response.Content.Contains("password"))
                {
                    throw new InvalidLoginException();
                }
                else
                {
                    throw new Exception(response.Content);
                }
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
            }
            else
            {
                IsEnabled = true;
                OnPropertyChanged("IsEnabled");
                throw new Exception(response.Content);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
