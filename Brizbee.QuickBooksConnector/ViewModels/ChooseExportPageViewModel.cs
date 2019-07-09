using Brizbee.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Brizbee.QuickBooksConnector.ViewModels
{
    public class ChooseExportPageViewModel : INotifyPropertyChanged
    {
        public string HelpText { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public ChooseExportPageViewModel()
        {
            var commit = Application.Current.Properties["SelectedCommit"] as Commit;
            HelpText = string.Format("Please choose where you want to export the {0} punches for the commit between {1} and {2}?",
                commit.PunchCount,
                commit.InAt.ToString("MMM dd, yyyy"),
                commit.OutAt.ToString("MMM dd, yyyy"));
            OnPropertyChanged("HelpText");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
