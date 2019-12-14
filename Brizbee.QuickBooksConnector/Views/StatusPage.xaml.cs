using Brizbee.QuickBooksConnector.ViewModels;
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
    /// Interaction logic for StatusPage.xaml
    /// </summary>
    public partial class StatusPage : Page
    {
        public StatusPage()
        {
            InitializeComponent();

            DataContext = new StatusPageViewModel();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as StatusPageViewModel).Export();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void TryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as StatusPageViewModel).Export();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartOverButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/LoginPage.xaml", UriKind.Relative));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
