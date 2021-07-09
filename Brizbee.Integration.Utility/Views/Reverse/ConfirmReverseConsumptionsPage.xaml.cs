//
//  ConfirmReverseConsumptionsPage.xaml.cs
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

using Brizbee.Integration.Utility.ViewModels.Reverse;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views.Reverse
{
    /// <summary>
    /// Interaction logic for ConfirmReverseConsumptionsPage.xaml
    /// </summary>
    public partial class ConfirmReverseConsumptionsPage : Page
    {
        public ConfirmReverseConsumptionsPage()
        {
            InitializeComponent();

            DataContext = new ConfirmReverseConsumptionsViewModel()
            {
                IsRefreshEnabled = false,
                IsContinueEnabled = false
            };
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties["SelectedSync"] =
                (DataContext as ConfirmReverseConsumptionsViewModel).SelectedSync;
            Application.Current.Properties["ReverseTransactionType"] = "Consumption";

            NavigationService.Navigate(new Uri("Views/Reverse/ReversePage.xaml", UriKind.Relative));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as ConfirmReverseConsumptionsViewModel).RefreshSyncs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Syncs", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as ConfirmReverseConsumptionsViewModel).RefreshSyncs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Syncs", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
