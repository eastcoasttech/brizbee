using Brizbee.Books.ViewModels;
using System;
using System.Linq;
using System.Windows;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for InvoicesWindow.xaml
/// </summary>
public partial class InvoicesWindow : Window
{
    private readonly InvoicesWindowViewModel _dataContext = new();

    public InvoicesWindow()
    {
        InitializeComponent();
        
        DataContext = _dataContext;
    }

    private async void InvoicesWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip = 0;
            await _dataContext.RefreshInvoiceAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ButtonPrevious_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip -= 1;
            await _dataContext.RefreshInvoiceAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ButtonNext_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip += 1;
            await _dataContext.RefreshInvoiceAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ButtonNew_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
