//
//  ReversePage.xaml.cs
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
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Brizbee.Integration.Utility.Views.Reverse
{
    /// <summary>
    /// Interaction logic for ReversePage.xaml
    /// </summary>
    public partial class ReversePage : Page
    {
        public ReversePage()
        {
            InitializeComponent();

            DataContext = new ReverseViewModel();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var thread = new Thread((DataContext as ReverseViewModel).Reverse);
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Reverse", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var thread = new Thread((DataContext as ReverseViewModel).Reverse);
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Reverse", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartOverButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }
    }
}
