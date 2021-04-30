using Brizbee.Integration.Utility.ViewModels.InventoryConsumptions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Brizbee.Integration.Utility.Views.InventoryConsumptions
{
    /// <summary>
    /// Interaction logic for OptionsPage.xaml
    /// </summary>
    public partial class OptionsPage : Page
    {
        public OptionsPage()
        {
            InitializeComponent();

            DataContext = new OptionsViewModel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as OptionsViewModel).Update();
            NavigationService.Navigate(new Uri("Views/InventoryConsumptions/ConfirmPage.xaml", UriKind.Relative));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (DataContext as OptionsViewModel).RefreshEnabled();
        }
    }
}
