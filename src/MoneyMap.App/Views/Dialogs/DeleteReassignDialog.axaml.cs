using MoneyMap.App.ViewModels.Pages;
using MoneyMap.App.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using MoneyMap.App.ViewModels;

namespace MoneyMap.App.Views.Dialogs;

public partial class DeleteReassignDialog : Window
{
    public DeleteReassignDialog()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowDialog(Window parent, DeleteReassignViewModel vm)
    {
        var dialog = new DeleteReassignDialog
        {
            DataContext = vm
        };

        var result = await dialog.ShowDialog<bool>(parent);
        return result;
    }
}


