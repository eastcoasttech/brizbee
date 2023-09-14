//
//  AccountsWindow.xaml.cs
//  Better Books by BRIZBEE
//
//  Copyright (C) 2023 East Coast Technology Services, LLC
//
//  This file is part of the BRIZBEE Common Library.
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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Brizbee.Books.ViewModels;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for AccountsWindow.xaml
/// </summary>
public partial class AccountsWindow : Window
{
    private DataGridColumn _currentAccountsSortColumn;

    private ListSortDirection _currentAccountsSortDirection;

    public AccountsWindow()
    {
        InitializeComponent();
        
        _currentAccountsSortColumn = DataGridAccounts.Columns[1];
        _currentAccountsSortDirection = ListSortDirection.Ascending;

        DataContext = new AccountsWindowViewModel();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await ((AccountsWindowViewModel)DataContext).RefreshAccountsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DataGridAccounts_OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        var columnIndex = e.Column.DisplayIndex;
        var viewModel = (DataContext as AccountsWindowViewModel)!;
        
        var direction = _currentAccountsSortDirection != ListSortDirection.Ascending ?
            ListSortDirection.Ascending : ListSortDirection.Descending;

        var sortAscending = direction == ListSortDirection.Ascending;

        viewModel.Sort(DataGridAccounts.Columns[columnIndex].SortMemberPath, sortAscending);

        // Clear sort on other columns and apply sort.
        foreach (var column in DataGridAccounts.Columns)
        {
            column.SortDirection = null;
        }
        DataGridAccounts.Columns[columnIndex].SortDirection = direction;

        // Apply the sort descriptions (for the indicators).
        DataGridAccounts.Items.SortDescriptions.Clear();
        DataGridAccounts.Items.SortDescriptions.Add(new SortDescription(DataGridAccounts.Columns[columnIndex].SortMemberPath, direction));

        // Record for usage later.
        _currentAccountsSortColumn = DataGridAccounts.Columns[columnIndex];
        _currentAccountsSortDirection = direction;
    }

    private void DataGridAccounts_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        throw new NotImplementedException();
    }
}
