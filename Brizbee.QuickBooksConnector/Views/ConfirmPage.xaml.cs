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
    /// Interaction logic for ConfirmPage.xaml
    /// </summary>
    public partial class ConfirmPage : Page
    {
        public ConfirmPage()
        {
            InitializeComponent();

            DataContext = new ConfirmPageViewModel();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/StatusPage.xaml", UriKind.Relative));
        }

        private void ViewPunchesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewPunchesWindow window = new ViewPunchesWindow();
            window.Owner = this.Parent as Window;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ViewPunchesWindow window = new ViewPunchesWindow();
            window.Owner = this.Parent as Window;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
            e.Handled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Views/CommitsPage.xaml", UriKind.Relative));
        }
    }
}
