using Brizbee.QuickBooksConnector.ViewModels;
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
using System.Windows.Shapes;

namespace Brizbee.QuickBooksConnector.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            DataContext = new LoginWindowViewModel()
            {
                IsEnabled = true
            };
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                (DataContext as LoginWindowViewModel).Login();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Sign In", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
