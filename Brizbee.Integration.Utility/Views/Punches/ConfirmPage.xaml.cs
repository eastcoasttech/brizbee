//
//  ConfirmPage.xaml.cs
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
    /// Interaction logic for ConfirmPage.xaml
    /// </summary>
    public partial class ConfirmPage : Page
    {
        public ConfirmPage()
        {
            InitializeComponent();

            DataContext = new ConfirmViewModel();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Punches/SyncPage.xaml", UriKind.Relative));
        }

        private void ViewPunchesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewPunchesWindow window = new ViewPunchesWindow();
            window.Owner = this.Parent as Window;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ViewPunchesWindow window = new ViewPunchesWindow();
            window.Owner = this.Parent as Window;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
            e.Handled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/Punches/LocksPage.xaml", UriKind.Relative));
        }
    }
}
