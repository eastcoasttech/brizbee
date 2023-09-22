using Brizbee.Books.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Brizbee.Core.Models.Accounting;

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
            await _dataContext.RefreshExpenseAccountsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Expense Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        try
        {
            await _dataContext.RefreshVendorsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could Not Load the Vendors", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        try
        {
            _dataContext.Skip = 0;
            await _dataContext.RefreshCheckAsync();
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
            await _dataContext.RefreshCheckAsync();
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
            await _dataContext.RefreshCheckAsync();
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

    private async void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        var (success, message) = await _dataContext.SaveCheckAsync();

        if (!success)
        {
            MessageBox.Show(message, $"Could Not Save the Check\r\n\r\n{message}", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

    private void ButtonDeleteLine_OnClick(object sender, RoutedEventArgs e)
    {
        _dataContext.DeleteCheckExpenseLine();
    }

    private void ButtonAddLine_OnClick(object sender, RoutedEventArgs e)
    {
        _dataContext.AddCheckExpenseLine();

        // Focus on new row and start editing the first cell.
        DataGridExpenses.CurrentCell = new DataGridCellInfo(
            DataGridExpenses.Items[^1], DataGridExpenses.Columns[0]);
        DataGridExpenses.BeginEdit();
    }

    private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
