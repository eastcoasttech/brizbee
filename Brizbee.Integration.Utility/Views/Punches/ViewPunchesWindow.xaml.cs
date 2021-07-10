//
//  ViewPunchesWindow.xaml.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2020 East Coast Technology Services, LLC
//
//  This file is part of BRIZBEE Integration Utility.
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

namespace Brizbee.Integration.Utility.Views.Punches
{
    /// <summary>
    /// Interaction logic for ViewPunchesWindow.xaml
    /// </summary>
    public partial class ViewPunchesWindow : Window
    {
        public ViewPunchesWindow()
        {
            InitializeComponent();

            DataContext = new ViewPunchesViewModel();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as ViewPunchesViewModel).RefreshPunches();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Load the Punches", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
