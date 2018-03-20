using Brizbee.Common.Models;
using Brizbee.QuickBooksConnector.Messages;
using Brizbee.QuickBooksConnector.ViewModels;
using Hellang.MessageBus;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Brizbee.QuickBooksConnector.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IHandle<RefreshCommitsMessage>, IHandle<SignedInMessage>
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel()
            {
                AccountButtonTitle = "SIGN IN TO YOUR ACCOUNT",
                AccountButtonStatus = "Not Signed In",
                ExportButtonTitle = "EXPORT TO QUICKBOOKS"
            };
            (Application.Current.Properties["MessageBus"] as MessageBus).Subscribe(this);
        }
        
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as MainWindowViewModel).Export();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(), ex.ToString(), EventLogEntryType.Warning);
                MessageBox.Show(ex.ToString(), "Could Not Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SignInOrOutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current.Properties["CurrentUser"] == null)
                {
                    var window = new LoginWindow();
                    window.Owner = this;
                    window.ShowDialog();
                }
                else
                {
                    (DataContext as MainWindowViewModel).SignOut();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CommitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshCommits();
        }

        public void Handle(RefreshCommitsMessage message)
        {
            RefreshCommits();
        }

        public void Handle(SignedInMessage message)
        {
            RefreshSignedInUser();
        }

        private async void RefreshCommits()
        {
            try
            {
                await (DataContext as MainWindowViewModel).RefreshCommits();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(), ex.ToString(), EventLogEntryType.Warning);
                MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshSignedInUser()
        {
            try
            {
                (DataContext as MainWindowViewModel).RefreshSignedInUser();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(), ex.ToString(), EventLogEntryType.Warning);
                MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
