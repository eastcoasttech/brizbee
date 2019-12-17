using Brizbee.Common.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Brizbee.QBExportUtility.ViewModels
{
    public class ConfirmPageViewModel : INotifyPropertyChanged
    {
        public string CommitId { get; set; }
        public string InAt { get; set; }
        public string OutAt { get; set; }
        public string PunchCount { get; set; }
        public string HelpText { get; set; }
        public string StatusText { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private RestClient client = Application.Current.Properties["Client"] as RestClient;

        public ConfirmPageViewModel()
        {
            var commit = Application.Current.Properties["SelectedCommit"] as Commit;
            InAt = commit.InAt.ToString("MMM dd, yyyy");
            OutAt = commit.OutAt.ToString("MMM dd, yyyy");
            CommitId = commit.Guid.ToString().Split('-')[4];
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
