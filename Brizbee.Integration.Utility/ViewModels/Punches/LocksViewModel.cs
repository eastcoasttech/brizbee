//
//  LocksViewModel.cs
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

using Brizbee.Common.Models;
using RestSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.Punches
{
    public class LocksViewModel : INotifyPropertyChanged
    {
        #region Public Fields

        public string CommitComboStatus { get; set; }
        public ObservableCollection<Commit> Commits { get; set; }
        public bool IsRefreshEnabled { get; set; }
        public bool IsContinueEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public Commit SelectedCommit { get; set; }

        #endregion

        #region Private Fields

        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        #endregion

        public async System.Threading.Tasks.Task RefreshCommits()
        {
            CommitComboStatus = "Downloading your locked punches";
            IsRefreshEnabled = false;
            IsContinueEnabled = false;
            OnPropertyChanged("CommitComboStatus");
            OnPropertyChanged("IsRefreshEnabled");
            OnPropertyChanged("IsContinueEnabled");

            // Build request to retrieve commits
            var request = new RestRequest("odata/Commits?$orderby=InAt", Method.GET);

            // Execute request to retrieve authenticated user
            var response = await client.ExecuteAsync<ODataResponse<Commit>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Commits = new ObservableCollection<Commit>(response.Data.Value);
                OnPropertyChanged("Commits");

                if (Commits.Count == 0)
                {
                    CommitComboStatus = "Uh oh, you do not have any locked punches";
                    SelectedCommit = null;
                    IsContinueEnabled = false;
                    OnPropertyChanged("CommitComboStatus");
                    OnPropertyChanged("SelectedCommit");
                    OnPropertyChanged("IsContinueEnabled");
                }
                else
                {
                    CommitComboStatus = "";
                    SelectedCommit = Commits[0];
                    IsContinueEnabled = true;
                    OnPropertyChanged("CommitComboStatus");
                    OnPropertyChanged("SelectedCommit");
                    OnPropertyChanged("IsContinueEnabled");
                }

                IsRefreshEnabled = true;
                OnPropertyChanged("IsRefreshEnabled");
            }
            else
            {
                Commits = new ObservableCollection<Commit>();
                IsRefreshEnabled = true;
                IsContinueEnabled = false;
                CommitComboStatus = response.ErrorMessage;
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsRefreshEnabled");
                OnPropertyChanged("IsContinueEnabled");
                OnPropertyChanged("CommitComboStatus");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
