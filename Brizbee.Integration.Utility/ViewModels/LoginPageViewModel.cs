//
//  LoginPageViewModel.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2019-2024 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Integration Utility.
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

using Brizbee.Core.Models;
using Brizbee.Integration.Utility.Exceptions;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.ComponentModel;
using System.Windows;
using Brizbee.Core.Security;
using System.Reflection;

namespace Brizbee.Integration.Utility.ViewModels
{
    public class LoginPageViewModel : INotifyPropertyChanged
    {
        public string EmailAddress { get; set; }
        public bool IsEnabled { get; set; }
        public string Password { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private RestClient _client;
        private readonly JsonSerializerSettings _settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public string Version { get; set; } = $"Version {Assembly.GetExecutingAssembly().GetName().Version}";

        public async System.Threading.Tasks.Task Login()
        {
            // Initialize the HTTP _client.
            _client = new RestClient("https://api-production-1.brizbee.com/",
                configureSerialization: s => s.UseSerializer(() => new JsonNetSerializer(_settings)));

            Application.Current.Properties["Client"] = _client;

            await LoadCredentials();
        }

        private async System.Threading.Tasks.Task LoadCredentials()
        {
            IsEnabled = false;
            OnPropertyChanged("IsEnabled");

            // Build request to authenticate user
            var request = new RestRequest("api/Auth/Authenticate", Method.Post);
            request.AddJsonBody(new
            {
                Method = "EMAIL",
                EmailAddress,
                EmailPassword = Password
            });

            // Execute request to authenticate user
            var response = await _client.ExecuteAsync<Authentication>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.Created))
            {
                // Save the authentication credentials for later
                Application.Current.Properties["Token"] = response.Data.Token;

                // Add the _client headers for authentication
                _client.AddDefaultHeader("Authorization", $"Bearer {response.Data.Token}");

                await LoadUser();
            }
            else
            {
                IsEnabled = true;
                OnPropertyChanged(nameof(IsEnabled));

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
            var request = new RestRequest("api/Auth/Me", Method.Get);

            // Execute request to retrieve authenticated user
            var response = await _client.ExecuteAsync<User>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                // Save the authenticated user for later
                Application.Current.Properties["CurrentUser"] = response.Data;
                IsEnabled = false;
                OnPropertyChanged(nameof(IsEnabled));
            }
            else
            {
                IsEnabled = true;
                OnPropertyChanged(nameof(IsEnabled));
                throw new Exception(response.Content);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
