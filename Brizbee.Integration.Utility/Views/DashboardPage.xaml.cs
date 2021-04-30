using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void SyncItemsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/InventoryItems/ConfirmPage.xaml", UriKind.Relative));
        }

        private void SyncAdjustmentsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/InventoryConsumptions/OptionsPage.xaml", UriKind.Relative));
        }

        private void SyncPunchesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/CommitsPage.xaml", UriKind.Relative));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
