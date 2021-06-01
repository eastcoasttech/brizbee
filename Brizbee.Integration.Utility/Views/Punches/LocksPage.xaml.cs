//
//  LocksPage.xaml.cs
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

using Brizbee.Integration.Utility.ViewModels.Punches;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views.Punches
{
    /// <summary>
    /// Interaction logic for LocksPage.xaml
    /// </summary>
    public partial class LocksPage : Page
    {
        public LocksPage()
        {
            InitializeComponent();

            DataContext = new LocksViewModel()
            {
                IsRefreshEnabled = false,
                IsContinueEnabled = false
            };
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties["SelectedCommit"] =
                (DataContext as LocksViewModel).SelectedCommit;

            NavigationService.Navigate(new Uri("Views/Punches/ConfirmPage.xaml", UriKind.Relative));
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as LocksViewModel).RefreshCommits();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Locks", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as LocksViewModel).RefreshCommits();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Locks", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }
    }
}
