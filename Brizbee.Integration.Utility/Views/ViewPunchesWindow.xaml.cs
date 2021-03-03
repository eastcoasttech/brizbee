using Brizbee.Integration.Utility.ViewModels;
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

namespace Brizbee.Integration.Utility.Views
{
    /// <summary>
    /// Interaction logic for ViewPunchesWindow.xaml
    /// </summary>
    public partial class ViewPunchesWindow : Window
    {
        public ViewPunchesWindow()
        {
            InitializeComponent();

            DataContext = new ViewPunchesViewModel();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as ViewPunchesViewModel).RefreshPunches();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Load the Punches", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
