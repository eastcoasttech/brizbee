using Brizbee.QBExportUtility.ViewModels.InventoryAdjustments;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Brizbee.QBExportUtility.Views.InventoryAdjustments
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as SyncViewModel).Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as SyncViewModel).Sync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartOverButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
