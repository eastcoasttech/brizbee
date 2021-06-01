//
//  DashboardPage.xaml.cs
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

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void SyncItemsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/InventoryItems/ConfirmPage.xaml", UriKind.Relative));
        }

        private void SyncAdjustmentsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/InventoryConsumptions/OptionsPage.xaml", UriKind.Relative));
        }

        private void SyncPunchesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Punches/LocksPage.xaml", UriKind.Relative));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        private void ReversePunchesLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Reverse/ConfirmReversePunchesPage.xaml", UriKind.Relative));
        }

        private void ReverseConsumptionsLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Reverse/ConfirmReverseConsumptionsPage.xaml", UriKind.Relative));
        }

        private void SyncProjectsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Projects/ConfirmPage.xaml", UriKind.Relative));
        }
    }
}
