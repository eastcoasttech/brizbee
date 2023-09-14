using System.Windows;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for HomeWindow.xaml
/// </summary>
public partial class HomeWindow : Window
{
    public HomeWindow()
    {
        InitializeComponent();
    }

    private void ButtonWriteChecks_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new WriteChecksWindow
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
