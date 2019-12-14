using Brizbee.QuickBooksConnector.ViewModels;
using RestSharp;
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

namespace Brizbee.QuickBooksConnector.Views
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            DataContext = new LoginPageViewModel()
            {
                IsEnabled = true,
                EmailAddress = "",
                Password = ""
            };
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as LoginPageViewModel).Login();
                NavigationService.Navigate(new Uri("Views/CommitsPage.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
