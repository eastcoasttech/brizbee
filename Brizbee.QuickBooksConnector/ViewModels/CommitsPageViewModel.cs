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
        public string CommitComboStatus { get; set; }
        public ObservableCollection<Commit> Commits { get; set; }
        public bool IsEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public Commit SelectedCommit { get; set; }

        private RestClient client = Application.Current.Properties["Client"] as RestClient;
        
        public async System.Threading.Tasks.Task RefreshCommits()
        {
            CommitComboStatus = "Downloading your commits";
            OnPropertyChanged("CommitComboStatus");

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
                    OnPropertyChanged("CommitComboStatus");
                    OnPropertyChanged("SelectedCommit");
                }
                else
                {
                    CommitComboStatus = "";
                    SelectedCommit = Commits[0];
                    OnPropertyChanged("CommitComboStatus");
                    OnPropertyChanged("SelectedCommit");
                }

                IsEnabled = true;
                OnPropertyChanged("IsEnabled");
            }
            else
            {
                Commits = new ObservableCollection<Commit>();
                IsEnabled = true;
                CommitComboStatus = response.ErrorMessage;
                OnPropertyChanged("Commits");
                OnPropertyChanged("IsEnabled");
                OnPropertyChanged("CommitComboStatus");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
