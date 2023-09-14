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

using Brizbee.Books.Serialization;
using Brizbee.Core.Models.Accounting;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Brizbee.Books.ViewModels;

public class WriteChecksWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public string Status { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public int Skip { get; set; } = 0;

    public Transaction? Transaction { get; set; }

    public string CheckNumber { get; set; } = string.Empty;
    
    public string PayToTheOrderOf { get; set; } = string.Empty;
    
    public string Memo { get; set; } = string.Empty;
    
    public DateOnly Date { get; set; }

    public string Amount { get; set; } = string.Empty;

    public string AmountVerbalized { get; set; } = string.Empty;
    
    public ObservableCollection<Expense>? Expenses { get; set; }

    private readonly RestClient? _client = Application.Current.Properties["Client"] as RestClient;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async System.Threading.Tasks.Task RefreshTransactionAsync()
    {
        IsEnabled = false;
        OnPropertyChanged(nameof(IsEnabled));

        Status = "Loading";
        OnPropertyChanged(nameof(Status));

        // Build request
        var request = new RestRequest("api/Accounting/Transactions");
        request.AddParameter("orderBy", "TRANSACTIONS/ENTERED_ON");
        request.AddParameter("orderByDirection", "ASC");
        request.AddParameter("filterByVoucherType", "CHK");
        request.AddParameter("skip", Skip);
        request.AddParameter("pageSize", 1);

        // Execute request
        var response = await _client!.ExecuteAsync(request);

        if (response is { ResponseStatus: ResponseStatus.Completed, StatusCode: System.Net.HttpStatusCode.OK })
        {
            var result = JsonSerializer.Deserialize<List<Transaction>>(response.Content!, _options); // Deserialize manually

            if (result != null && result.Any())
            {
                Transaction = result.FirstOrDefault();
                OnPropertyChanged(nameof(Transaction));

                CheckNumber = Transaction!.ReferenceNumber;
                OnPropertyChanged(nameof(CheckNumber));

                Date = new DateOnly(Transaction.EnteredOn.Year, Transaction.EnteredOn.Month, Transaction.EnteredOn.Day);
                OnPropertyChanged(nameof(Date));

                Memo = Transaction.Description;
                OnPropertyChanged(nameof(Memo));
            }

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Done";
            OnPropertyChanged(nameof(Status));
        }
        else
        {
            Transaction = null;
            OnPropertyChanged(nameof(Transaction));

            IsEnabled = true;
            OnPropertyChanged(nameof(IsEnabled));

            Status = "Failed to Load Check";
            OnPropertyChanged(nameof(Status));
        }
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
