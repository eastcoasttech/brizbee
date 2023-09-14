//
//  LoginWindow.xaml.cs
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

using Brizbee.Books.Exceptions;
using Brizbee.Books.ViewModels;
using System;
using System.Windows;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        DataContext = new LoginWindowViewModel()
        {
            IsEnabled = true,
            EmailAddress = "joshuasmartin@gowitheast.com",
            Password = "iuhsdf87hasdfg7u*&^&^tg23ruykgasdfjhka"
        };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ((LoginWindowViewModel)DataContext).Login();

            var window = new MainWindow();
            window.Show();
            Hide();
        }
        catch (InvalidLoginException)
        {
            MessageBox.Show("Your Email address and password do not match an account. Please verify that your password is correct.", "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
