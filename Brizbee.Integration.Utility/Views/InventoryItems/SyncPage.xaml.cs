using Brizbee.Integration.Utility.ViewModels.InventoryItems;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Brizbee.Integration.Utility.Views.InventoryItems
{
    /// <summary>
    /// Interaction logic for SyncPage.xaml
    /// </summary>
    public partial class SyncPage : Page
    {
        public SyncPage()
        {
            InitializeComponent();

            DataContext = new SyncViewModel();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as SyncViewModel).Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void TryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as SyncViewModel).Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartOverButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
