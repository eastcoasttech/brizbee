//
//  MainWindow.xaml.cs
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

using System.Windows;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuItemFileExit_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown(0);
    }

    private void MenuItemListsChartOfAccounts_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new AccountsWindow()
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = new HomeWindow()
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
