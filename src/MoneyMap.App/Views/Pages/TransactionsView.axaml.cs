using MoneyMap.App.ViewModels.Pages;
using Avalonia.Controls;
using Avalonia.Input;
using MoneyMap.App.ViewModels;

namespace MoneyMap.App.Views.Pages;

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

