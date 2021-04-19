using Brizbee.Integration.Utility.ViewModels;
using System;
using System.Collections.Generic;
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

namespace Brizbee.Integration.Utility.Views
{
    /// <summary>
    /// Interaction logic for CommitsPage.xaml
    /// </summary>
    public partial class CommitsPage : Page
    {
        public CommitsPage()
        {
            InitializeComponent();

            DataContext = new CommitsPageViewModel()
            {
                IsRefreshEnabled = false,
                IsContinueEnabled = false
            };
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties["SelectedCommit"] =
                (DataContext as CommitsPageViewModel).SelectedCommit;

            NavigationService.Navigate(new Uri("Views/ConfirmPage.xaml", UriKind.Relative));
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as CommitsPageViewModel).RefreshCommits();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Commits", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as CommitsPageViewModel).RefreshCommits();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Refresh Commits", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }
    }
}
