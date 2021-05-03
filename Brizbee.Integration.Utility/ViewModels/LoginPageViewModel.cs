//
//  LoginPageViewModel.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Database Management.
//
//  This program is free software: you can redistribute
//  it and/or modify it under the terms of the GNU General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will
//  be useful, but WITHOUT ANY WARRANTY; without even the implied
//  warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//  See the GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.
//  If not, see <https://www.gnu.org/licenses/>.
//

using Brizbee.Common.Models;
using Brizbee.Common.Security;
using Brizbee.Integration.Utility.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels
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
            client = new RestClient("https://app-brizbee-prod.azurewebsites.net/");
            Application.Current.Properties["Client"] = client;

            await LoadCredentials();
        }

        public async System.Threading.Tasks.Task<string> CheckLatestVersion()
        {
            // Build request to check latest version of software
            var versionClient = new RestClient("https://ects1.blob.core.windows.net/");
            var versionRequest = new RestRequest("brizbee-public/latest.json", Method.GET);

            // Execute request to check the latest version
            var versionResponse = await versionClient.ExecuteAsync(versionRequest);
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
