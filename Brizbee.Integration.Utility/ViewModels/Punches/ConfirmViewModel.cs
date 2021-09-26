//
//  ConfirmViewModel.cs
//  BRIZBEE Integration Utility
//
//  Copyright (C) 2019-2021 East Coast Technology Services, LLC
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
using System.ComponentModel;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.Punches
{
    public class ConfirmViewModel : INotifyPropertyChanged
    {
        public string CommitId { get; set; }
        public string InAt { get; set; }
        public string OutAt { get; set; }
        public string PunchCount { get; set; }
        public string HelpText { get; set; }
        public string StatusText { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        public ConfirmViewModel()
        {
            var commit = Application.Current.Properties["SelectedCommit"] as Commit;
            InAt = commit.InAt.ToString("MMM dd, yyyy");
            OutAt = commit.OutAt.ToString("MMM dd, yyyy");
            CommitId = commit.Id.ToString();
            PunchCount = commit.PunchCount.ToString();
            OnPropertyChanged("InAt");
            OnPropertyChanged("OutAt");
            OnPropertyChanged("CommitId");
            OnPropertyChanged("PunchCount");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
