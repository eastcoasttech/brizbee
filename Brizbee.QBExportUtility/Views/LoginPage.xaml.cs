using Brizbee.QBExportUtility.ViewModels;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace Brizbee.QBExportUtility.Views
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

        private async Task CheckLatestVersion()
        {
            try
            {
                string sRunningVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var sLatestVersion = await (DataContext as LoginPageViewModel).CheckLatestVersion();
                if (!string.IsNullOrEmpty(sRunningVersion) && !string.IsNullOrEmpty(sLatestVersion))
                {
                    var latestVersion = new Version(sLatestVersion);
                    var runningVersion = new Version(sRunningVersion);

                    if (latestVersion > runningVersion)
                    {
                        MessageBox.Show("You are not running the latest version! Please download the latest version at https://www.brizbee.com/", "Oops!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch
            {
                // Do not need to warn the user
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckLatestVersion();
        }
    }
}
