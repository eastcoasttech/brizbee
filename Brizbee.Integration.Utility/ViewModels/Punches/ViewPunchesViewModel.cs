//
//  ViewPunchesViewModel.cs
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
using Newtonsoft.Json;
using RestSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Brizbee.Integration.Utility.ViewModels.Punches
{
    public class ViewPunchesViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private ICommand firstCommand;
        private ICommand previousCommand;
        private ICommand nextCommand;
        private ICommand lastCommand;
        private int itemsSkip = 0;
        private int itemsTop = 20;
        private int itemsCount = 0;
        private string PunchesSortDirection = "asc";
        private string PunchesSortColumn = "InAt";
        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        #endregion

        #region Public Fields

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Punch> Punches { get; set; }
        public bool IsRefreshEnabled { get; set; }

        public int PunchesStart { get { return itemsSkip + 1; } }

        public int PunchesEnd { get { return itemsSkip + itemsTop < itemsCount ? itemsSkip + itemsTop : itemsCount; } }

        public int PunchesCount { get { return itemsCount; } }

        /// <summary>
        /// Gets the command for moving to the first page of objects.
        /// </summary>
        public ICommand FirstCommand
        {
            get
            {
                if (firstCommand == null)
                {
                    firstCommand = new RelayCommand
                    (
                    param =>
                    {
                        itemsSkip = 0;
                        new Task<System.Threading.Tasks.Task>(RefreshPunches).Start();
                    },
                    param =>
                    {
                        return itemsSkip - itemsTop >= 0 ? true : false;
                    }
                    );
                }

                return firstCommand;
            }
        }

        /// <summary>
        /// Gets the command for moving to the previous page of objects.
        /// </summary>
        public ICommand PreviousCommand
        {
            get
            {
                if (previousCommand == null)
                {
                    previousCommand = new RelayCommand
                    (
                    param =>
                    {
                        itemsSkip -= itemsTop;
                        new Task<System.Threading.Tasks.Task>(RefreshPunches).Start();
                    },
                    param =>
                    {
                        return itemsSkip - itemsTop >= 0 ? true : false;
                    }
                    );
                }

                return previousCommand;
            }
        }

        /// <summary>
        /// Gets the command for moving to the next page of objects.
        /// </summary>
        public ICommand NextCommand
        {
            get
            {
                if (nextCommand == null)
                {
                    nextCommand = new RelayCommand
                    (
                    param =>
                    {
                        itemsSkip += itemsTop;
                        new Task<System.Threading.Tasks.Task>(RefreshPunches).Start();
                    },
                    param =>
                    {
                        return itemsSkip + itemsTop < itemsCount ? true : false;
                    }
                    );
                }

                return nextCommand;
            }
        }

        /// <summary>
        /// Gets the command for moving to the last page of objects.
        /// </summary>
        public ICommand LastCommand
        {
            get
            {
                if (lastCommand == null)
                {
                    lastCommand = new RelayCommand
                    (
                    param =>
                    {
                        itemsSkip = (itemsCount / itemsTop - 1) * itemsTop;
                        itemsSkip += itemsCount % itemsTop == 0 ? 0 : itemsTop;
                        new Task<System.Threading.Tasks.Task>(RefreshPunches).Start();
                    },
                    param =>
                    {
                        return itemsSkip + itemsTop < itemsCount ? true : false;
                    }
                    );
                }

                return lastCommand;
            }
        }

        #endregion

        public async System.Threading.Tasks.Task RefreshPunches()
        {
            var commit = Application.Current.Properties["SelectedCommit"] as Commit;

            // Build request to retrieve punches
            var request = new RestRequest("odata/Punches", Method.GET);
            request.AddParameter("$count", "true");
            request.AddParameter("$expand", "User,Task($expand=Job($expand=Customer))");
            request.AddParameter("$filter", string.Format("CommitId eq {0}", commit.Id));
            request.AddParameter("$top", "20");
            request.AddParameter("$skip", itemsSkip);
            request.AddParameter("$orderby", string.Format("{0} {1}", PunchesSortColumn, PunchesSortDirection));

            // Execute request to retrieve punches
            var response = await client.ExecuteTaskAsync(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                var result = JsonConvert.DeserializeObject<ODataResponse<Punch>>(response.Content); // Deserialize manually
                Punches = new ObservableCollection<Punch>(result.Value);
                itemsCount = result.Count;
                OnPropertyChanged("Punches");
                OnPropertyChanged("PunchesStart");
                OnPropertyChanged("PunchesEnd");
                OnPropertyChanged("PunchesCount");

                if (Punches.Count == 0)
                {
                    // alert?
                }

                IsRefreshEnabled = true;
                OnPropertyChanged("IsRefreshEnabled");
            }
            else
            {
                Punches = new ObservableCollection<Punch>();
                IsRefreshEnabled = true;
                OnPropertyChanged("Punches");
                OnPropertyChanged("IsRefreshEnabled");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
