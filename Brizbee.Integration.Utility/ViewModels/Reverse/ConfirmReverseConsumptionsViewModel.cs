//
//  ConfirmReverseConsumptionsViewModel.cs
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

using Brizbee.Common.Models;
using RestSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.Reverse
{
    public class ConfirmReverseConsumptionsViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public string Status { get; set; }
        public ObservableCollection<QBDInventoryConsumptionSync> Syncs { get; set; }
        public bool IsRefreshEnabled { get; set; }
        public bool IsContinueEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public QBDInventoryConsumptionSync SelectedSync { get; set; }
        #endregion

        #region Private Fields
        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        #endregion

        public async System.Threading.Tasks.Task RefreshSyncs()
        {
            Status = "Downloading your consumption syncs";
            IsRefreshEnabled = false;
            IsContinueEnabled = false;
            OnPropertyChanged("Status");
            OnPropertyChanged("IsRefreshEnabled");
            OnPropertyChanged("IsContinueEnabled");

            // Build request to get syncs.
            var request = new RestRequest("api/QBDInventoryConsumptionSyncs?orderby=QBDINVENTORYCONSUMPTIONSYNCS/CREATEDAT&orderByDirection=DESC", Method.GET);

            // Execute request.
            var response = await client.ExecuteAsync<List<QBDInventoryConsumptionSync>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Syncs = new ObservableCollection<QBDInventoryConsumptionSync>(response.Data);
                OnPropertyChanged("Syncs");

                if (Syncs.Count == 0)
                {
                    Status = "Uh oh, you do not have any consumption syncs";
                    SelectedSync = null;
                    IsContinueEnabled = false;
                    OnPropertyChanged("Status");
                    OnPropertyChanged("SelectedSync");
                    OnPropertyChanged("IsContinueEnabled");
                }
                else
                {
                    Status = "";
                    SelectedSync = Syncs[0];
                    IsContinueEnabled = true;
                    OnPropertyChanged("Status");
                    OnPropertyChanged("SelectedSync");
                    OnPropertyChanged("IsContinueEnabled");
                }

                IsRefreshEnabled = true;
                OnPropertyChanged("IsRefreshEnabled");
            }
            else
            {
                Syncs = new ObservableCollection<QBDInventoryConsumptionSync>();
                IsRefreshEnabled = true;
                IsContinueEnabled = false;
                Status = response.ErrorMessage;
                OnPropertyChanged("Syncs");
                OnPropertyChanged("IsRefreshEnabled");
                OnPropertyChanged("IsContinueEnabled");
                OnPropertyChanged("Status");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
