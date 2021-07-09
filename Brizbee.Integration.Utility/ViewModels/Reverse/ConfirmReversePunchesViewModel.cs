//
//  ConfirmReversePunchesViewModel.cs
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
    public class ConfirmReversePunchesViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public string Status { get; set; }
        public ObservableCollection<QuickBooksDesktopExport> Syncs { get; set; }
        public bool IsRefreshEnabled { get; set; }
        public bool IsContinueEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public QuickBooksDesktopExport SelectedSync { get; set; }
        #endregion

        #region Private Fields
        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        #endregion

        public async System.Threading.Tasks.Task RefreshSyncs()
        {
            Status = "Downloading your exports";
            IsRefreshEnabled = false;
            IsContinueEnabled = false;
            OnPropertyChanged("Status");
            OnPropertyChanged("IsRefreshEnabled");
            OnPropertyChanged("IsContinueEnabled");

            // Build request to get syncs.
            var request = new RestRequest("odata/QuickBooksDesktopExports?$count=true&$top=20&$skip=0&$orderby=CreatedAt ASC", Method.GET);

            // Execute request.
            var response = await client.ExecuteAsync<ODataResponse<QuickBooksDesktopExport>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Syncs = new ObservableCollection<QuickBooksDesktopExport>(response.Data.Value);
                OnPropertyChanged("Syncs");

                if (Syncs.Count == 0)
                {
                    Status = "Uh oh, you do not have any QuickBooks Desktop exports";
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
                Syncs = new ObservableCollection<QuickBooksDesktopExport>();
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
