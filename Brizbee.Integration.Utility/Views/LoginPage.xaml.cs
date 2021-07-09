//
//  LoginPage.xaml.cs
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

using Brizbee.Integration.Utility.Exceptions;
using Brizbee.Integration.Utility.ViewModels;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            DataContext = new LoginPageViewModel()
            {
                IsEnabled = true,
                EmailAddress = "",
                Password = ""
            };
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as LoginPageViewModel).Login();
                NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
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
}
