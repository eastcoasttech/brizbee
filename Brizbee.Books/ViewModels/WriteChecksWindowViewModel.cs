//
//  WriteChecksWindowViewModel.cs
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Brizbee.Core.Models;

namespace Brizbee.Books.ViewModels;

public class WriteChecksWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Status { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int Skip { get; set; } = 0;

    public Check? Check { get; set; }

    public string CheckNumber { get; set; } = string.Empty;
    
    public string Memo { get; set; } = string.Empty;
    
    public DateOnly Date { get; set; }

    public string Amount { get; set; } = string.Empty;

    public string AmountVerbalized { get; set; } = string.Empty;

    public int? VendorId { get; set; }
    
    public ObservableCollection<Account>? ExpenseAccounts { get; set; }
    
    public ObservableCollection<Vendor>? Vendors { get; set; }
    
    public ObservableCollection<CheckExpenseLine>? CheckExpenseLines { get; set; }

    public CheckExpenseLine? SelectedCheckExpenseLine { get; set; }

    private readonly RestClient? _client = Application.Current.Properties["Client"] as RestClient;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async System.Threading.Tasks.Task RefreshExpenseAccountsAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Accounts");
        request.AddParameter("orderBy", "ACCOUNTS/NAME");
        request.AddParameter("orderByDirection", "ASC");
        request.AddParameter("skip", Skip);
        request.AddParameter("pageSize", 1000);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Account>>(response.Content!, _options); // Deserialize manually
            ExpenseAccounts = new ObservableCollection<Account>(result!);
            OnPropertyChanged(nameof(ExpenseAccounts));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            ExpenseAccounts = null;
            OnPropertyChanged(nameof(ExpenseAccounts));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Failed to Load Expense Accounts";
            OnPropertyChanged(nameof(Status));
        }
    }

    public async System.Threading.Tasks.Task RefreshVendorsAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Vendors");
        request.AddParameter("orderBy", "VENDORS/NAME");
        request.AddParameter("orderByDirection", "ASC");
        request.AddParameter("skip", Skip);
        request.AddParameter("pageSize", 1000);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Vendor>>(response.Content!, _options); // Deserialize manually
            Vendors = new ObservableCollection<Vendor>(result!);
            OnPropertyChanged(nameof(Vendors));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            Vendors = null;
            OnPropertyChanged(nameof(Vendors));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Failed to Load Vendors";
            OnPropertyChanged(nameof(Status));
        }
    }

    public void AddCheckExpenseLine()
    {
        CheckExpenseLines ??= new ObservableCollection<CheckExpenseLine>();

        CheckExpenseLines!.Add(new CheckExpenseLine());

        OnPropertyChanged(nameof(CheckExpenseLines));
    }

    public void DeleteCheckExpenseLine()
    {
        if (SelectedCheckExpenseLine == null)
        {
            return;
        }

        CheckExpenseLines!.Remove(SelectedCheckExpenseLine);

        OnPropertyChanged(nameof(CheckExpenseLines));
    }

    public async System.Threading.Tasks.Task RefreshCheckAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Checks");
        request.AddParameter("orderBy", "CHECKS/ENTERED_ON");
        request.AddParameter("orderByDirection", "ASC");
        request.AddParameter("skip", Skip);
        request.AddParameter("pageSize", 1);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Check>>(response.Content!, _options); // Deserialize manually

            if (result != null && result.Any())
            {
                Check = result.FirstOrDefault();
                OnPropertyChanged(nameof(Check));

                CheckNumber = Check!.Number;
                OnPropertyChanged(nameof(CheckNumber));

                Date = new DateOnly(Check.EnteredOn.Year, Check.EnteredOn.Month, Check.EnteredOn.Day);
                OnPropertyChanged(nameof(Date));

                Memo = Check.Memo;
                OnPropertyChanged(nameof(Memo));
            }

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            Check = new Check();
            OnPropertyChanged(nameof(Check));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
    }

    public async Task<(bool Success, string Message)> SaveCheckAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Saving";
        OnPropertyChanged(nameof(Status));
        
        // Build the payload.
        var payload = new
        {
            Number = CheckNumber,
            EnteredOn = Date,
            VendorId = 0,
            Memo,
            CheckExpenseLines = new List<object>()
        };

        foreach (var checkExpenseLine in CheckExpenseLines!)
        {
            payload.CheckExpenseLines.Add(new
            {
                checkExpenseLine.AccountId,
                checkExpenseLine.Amount
            });
        }

        // Build request
        var request = new RestRequest("api/Accounting/Checks", Method.Post);
        request.AddJsonBody(payload);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));

            return (true, string.Empty);
        }

        ExpenseAccounts = null;
        OnPropertyChanged(nameof(ExpenseAccounts));

        IsEnabled = true;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Failed to Save Check";
        OnPropertyChanged(nameof(Status));

        return (false, response.Content!);
    }

    public void UpdateVerbalizedAmount()
    {
        try
        {
            var parsed = decimal.Parse(Amount);
            var verbalized = ToVerbalCurrency(parsed);
            AmountVerbalized = verbalized;
            OnPropertyChanged(nameof(AmountVerbalized));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static string ToVerbalCurrency(decimal value)
    {
        var valueString = value.ToString("N2");
        var decimalString = valueString[(valueString.LastIndexOf('.') + 1)..];
        var wholeString = valueString[..valueString.LastIndexOf('.')];

        var valueArray = wholeString.Split(',');

        var unitsMap = new[] { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        var tensMap = new[] { "", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        var placeMap = new[] { "", " thousand ", " million ", " billion ", " trillion " };

        var outList = new List<string>();

        var placeIndex = 0;

        for (var i = valueArray.Length - 1; i >= 0; i--)
        {
            var intValue = int.Parse(valueArray[i]);
            var tensValue = intValue % 100;

            string tensString;
            if (tensValue < unitsMap.Length) tensString = unitsMap[tensValue];
            else tensString = tensMap[(tensValue - tensValue % 10) / 10] + " " + unitsMap[tensValue % 10];

            var fullValue = string.Empty;
            if (intValue >= 100) fullValue = unitsMap[(intValue - intValue % 100) / 100] + " hundred " + tensString + placeMap[placeIndex++];
            else if (intValue != 0) fullValue = tensString + placeMap[placeIndex++];
            else placeIndex++;

            outList.Add(fullValue);
        }

        var intCentsValue = int.Parse(decimalString);

        string centsString;
        if (intCentsValue < unitsMap.Length) centsString = unitsMap[intCentsValue];
        else centsString = tensMap[(intCentsValue - intCentsValue % 10) / 10] + " " + unitsMap[intCentsValue % 10];

        if (intCentsValue == 0) centsString = "zero";

        var output = string.Empty;
        for (var i = outList.Count - 1; i >= 0; i--) output += outList[i];
        output += " dollars and " + centsString + " cents";

        return output.ToUpper();
    }
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
