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
    /// Interaction logic for ChooseExportPage.xaml
    /// </summary>
    public partial class ChooseExportPage : Page
    {
        public ChooseExportPage()
        {
            InitializeComponent();

            DataContext = new ChooseExportPageViewModel();
        }

        private async void QuickBooksButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (DataContext as ChooseExportPageViewModel).Export();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Application.Current.Properties["EventSource"].ToString(), ex.ToString(), EventLogEntryType.Warning);
                MessageBox.Show(ex.ToString(), "Could Not Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
