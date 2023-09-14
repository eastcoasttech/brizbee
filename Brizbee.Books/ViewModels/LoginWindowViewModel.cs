//
//  LoginWindowViewModel.cs
//
//  Copyright (C) 2023 East Coast Technology Services, LLC
//
//  This file is part of Better Books by BRIZBEE.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as
//  published by the Free Software Foundation, either version 3 of the
//  License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using RestSharp;
using System;
using System.ComponentModel;
using System.Windows;
using Brizbee.Books.Exceptions;
using Brizbee.Books.Serialization;
using Brizbee.Core.Models;
using System.Text.Json;

namespace Brizbee.Books.ViewModels;

public class LoginWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string EmailAddress { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;

    public string Password { get; set; } = string.Empty;

    private RestClient? _client;
    
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async System.Threading.Tasks.Task Login()
    {
        // Initialize the HTTP client.
        var client = new RestClient("https://app-brizbee-api-prod-slot-1.azurewebsites.net/");

        _client = client;

        Application.Current.Properties["Client"] = client;

        await LoadCredentials();
    }

    private async System.Threading.Tasks.Task LoadCredentials()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        // Build request to authenticate user
        var request = new RestRequest("api/Auth/Authenticate", Method.Post);
        request.AddJsonBody(new
        {
            Method = "EMAIL",
            EmailAddress,
            EmailPassword = Password
        });

        // Execute request to authenticate user
        var response = await _client!.ExecuteAsync<Authentication>(request);
        if ((response.ResponseStatus == ResponseStatus.Completed) &&
                (response.StatusCode == System.Net.HttpStatusCode.Created))
        {
            // Save the authentication credentials for later
            Application.Current.Properties["Token"] = response.Data!.Token;

            // Add the _client headers for authentication
            _client!.AddDefaultHeader("Authorization", $"Bearer {response.Data.Token}");

            await LoadUser();
        }
        else
        {
            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            if (response.Content!.Contains("password"))
            {
                throw new InvalidLoginException("Cannot login with those credentials.");
            }

            throw new Exception(response.Content);
        }
    }

    private async System.Threading.Tasks.Task LoadUser()
    {
        // Build request to retrieve authenticated user
        var request = new RestRequest("api/Auth/Me");

        // Execute request to retrieve authenticated user
        var response = await _client!.ExecuteAsync<User>(request);
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
