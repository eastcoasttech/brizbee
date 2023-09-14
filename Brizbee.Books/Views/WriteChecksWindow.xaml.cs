using Brizbee.Books.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Brizbee.Books.Views;

/// <summary>
/// Interaction logic for WriteChecksWindow.xaml
/// </summary>
public partial class WriteChecksWindow : Window
{
    private readonly WriteChecksWindowViewModel _dataContext = new();

    public WriteChecksWindow()
    {
        InitializeComponent();

        DataContext = _dataContext;
    }

    private async void WriteChecksWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip = 0;
            await _dataContext.RefreshTransactionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Check", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ButtonPrevious_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip -= 1;
            await _dataContext.RefreshTransactionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Check", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ButtonNext_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _dataContext.Skip += 1;
            await _dataContext.RefreshTransactionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Check", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Task.Delay(500);
            _dataContext.UpdateVerbalizedAmount();
        });
    }
}
