//
//  InvoicesWindowViewModel.cs
//  Better Books by BRIZBEE
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
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Brizbee.Books.ViewModels;

public class InvoicesWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Status { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int Skip { get; set; } = 0;

    public Invoice? Invoice { get; set; }
    
    private readonly RestClient? _client = Application.Current.Properties["Client"] as RestClient;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };
        
    public async System.Threading.Tasks.Task RefreshInvoiceAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Invoices");
        request.AddParameter("orderBy", "INVOICES/ENTERED_ON");
        request.AddParameter("orderByDirection", "ASC");
        request.AddParameter("filterByVoucherType", "INV");
        request.AddParameter("skip", Skip);
        request.AddParameter("pageSize", 1);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Invoice>>(response.Content!, _options); // Deserialize manually

            if (result != null && result.Any())
            {
                Invoice = result.FirstOrDefault();
                OnPropertyChanged(nameof(Invoice));
            }

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            Invoice = null;
            OnPropertyChanged(nameof(Invoice));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Failed to Load Invoice";
            OnPropertyChanged(nameof(Status));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
