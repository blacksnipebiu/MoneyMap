using Avalonia.Controls;
using Avalonia.Input;
using Bookkeeping.App.ViewModels;

namespace Bookkeeping.App.Views;

public partial class TransactionsView : UserControl
{
    public TransactionsView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TransactionsViewModel vm)
        {
            vm.SearchCommand.Execute(null);
        }
    }
}