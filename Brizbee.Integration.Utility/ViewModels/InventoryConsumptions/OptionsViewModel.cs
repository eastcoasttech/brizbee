//
//  OptionsViewModel.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.InventoryConsumptions
{
    public class OptionsViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public string SelectedMethod { get; set; } = "Sales Receipt";
        public string SelectedValue { get; set; } = "Purchase Cost";
        public bool IsEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public List<string> Values
        {
            get
            {
                return new List<string>() { "Zero", "Sales Price", "Purchase Cost" };
            }
        }
        public List<string> Methods
        {
            get
            {
                return new List<string>() { "Sales Receipt", "Bill", "Inventory Adjustment" };
            }
        }
        #endregion

        public void Update()
        {
            Application.Current.Properties["SelectedMethod"] = SelectedMethod;
            Application.Current.Properties["SelectedValue"] = SelectedValue;
        }

        public void RefreshEnabled()
        {
            if (SelectedMethod == "Sales Receipt")
                IsEnabled = true;
            else if (SelectedMethod == "Bill")
                IsEnabled = true;
            else if (SelectedMethod == "Inventory Adjustment")
                IsEnabled = false;

            SelectedValue = "Purchase Cost";
            OnPropertyChanged("SelectedValue");
            OnPropertyChanged("IsEnabled");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
