using Brizbee.Common.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Brizbee.QuickBooksConnector.ViewModels
{
    public class CommitsPageViewModel : INotifyPropertyChanged
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
            CommitComboStatus = "Downloading your commits";
            IsRefreshEnabled = false;
            IsContinueEnabled = false;
            OnPropertyChanged("CommitComboStatus");
            OnPropertyChanged("IsRefreshEnabled");
            OnPropertyChanged("IsContinueEnabled");

            // Build request to retrieve commits
            var request = new RestRequest("odata/Commits?$orderby=InAt", Method.GET);

            // Execute request to retrieve authenticated user
            var response = await client.ExecuteTaskAsync<ODataResponse<Commit>>(request);
            if ((response.ResponseStatus == ResponseStatus.Completed) &&
                    (response.StatusCode == System.Net.HttpStatusCode.OK))
            {
                Commits = new ObservableCollection<Commit>(response.Data.Value);
                OnPropertyChanged("Commits");

                if (Commits.Count == 0)
                {
                    CommitComboStatus = "Uh oh, you do not have any commits";
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
