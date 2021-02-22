using Brizbee.QBExportUtility.Exceptions;
using Brizbee.QBExportUtility.ViewModels;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

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
                NavigationService.Navigate(new Uri("Views/DashboardPage.xaml", UriKind.Relative));
            }
            catch (InvalidLoginException)
            {
                MessageBox.Show("Your Email address and password do not match an account. Please verify that your password is correct.", "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
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
