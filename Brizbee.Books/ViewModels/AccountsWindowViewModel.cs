//
//  AccountsWindowViewModel.cs
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

using Brizbee.Core.Models.Accounting;
using RestSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;

namespace Brizbee.Books.ViewModels;

public class AccountsWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Account>? Accounts { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    
    private string _accountsOrderByDirection = "asc";

    private string _accountsOrderByColumn = "Name";

    private readonly RestClient? _client = Application.Current.Properties["Client"] as RestClient;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public async System.Threading.Tasks.Task RefreshAccountsAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Accounts");
        request.AddParameter("orderBy", _accountsOrderByColumn);
        request.AddParameter("orderByDirection", _accountsOrderByDirection);

        // Execute request
        var response = await _client!.ExecuteAsync(request);
        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Account>>(response.Content!, _options); // Deserialize manually
            Accounts = new ObservableCollection<Account>(result!);
            OnPropertyChanged(nameof(Accounts));
            
            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            Accounts = new ObservableCollection<Account>();
            OnPropertyChanged(nameof(Accounts));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Failed to Load Accounts";
            OnPropertyChanged(nameof(Status));
        }
    }
        
    /// <summary>
    /// Apply sorting and refresh.
    /// </summary>
    public async void Sort(string sortColumn, bool ascending)
    {
        _accountsOrderByColumn = sortColumn;

        _accountsOrderByDirection = ascending ? "asc" : "desc";

        await RefreshAccountsAsync();
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
