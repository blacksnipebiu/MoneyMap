using Bookkeeping.App.ViewModels.Pages;
using Bookkeeping.App.ViewModels.Dialogs;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Bookkeeping.App.ViewModels;

namespace Bookkeeping.App.Views.Dialogs;

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


