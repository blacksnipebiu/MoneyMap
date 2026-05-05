using MoneyMap.App.ViewModels.Pages;
using MoneyMap.App.ViewModels.Dialogs;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MoneyMap.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyMap.App.Views.Dialogs;

public partial class CategoryEditDialog : Window
{
    private CategoryEditViewModel? _viewModel;

    public CategoryEditDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public static async Task<CategoryEditViewModel?> ShowDialogAsync(Window parent, CategoryEditViewModel vm)
    {
        var dialog = new CategoryEditDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        vm.InitializeForAdd(Core.Enums.TransactionType.Expense);
        dialog.DataContext = vm;
        dialog._viewModel = vm;
        dialog.Owner = parent;
        dialog.UpdateTitle();

        return await dialog.ShowDialog<CategoryEditViewModel?>(parent);
    }

    public static async Task<CategoryEditViewModel?> ShowDialogAsync(Window parent, CategoriesViewModel.CategoryItemViewModel category, IServiceScopeFactory scopeFactory)
    {
        var dialog = new CategoryEditDialog
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var vm = new CategoryEditViewModel(scopeFactory);
        vm.InitializeForEdit(category);
        dialog.DataContext = vm;
        dialog._viewModel = vm;
        dialog.Owner = parent;
        dialog.UpdateTitle();

        return await dialog.ShowDialog<CategoryEditViewModel?>(parent);
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is CategoryEditViewModel vm)
        {
            _viewModel = vm;
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        if (_viewModel != null)
        {
            Title = _viewModel.IsEditMode ? "编辑分类" : "添加分类";
            TitleText.Text = _viewModel.IsEditMode ? "编辑分类" : "添加分类";
        }
    }

    private async void OnSelectIconClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null || Owner is not Window ownerWindow) return;

        var selectedEmoji = await EmojiPickerDialog.ShowDialogAsync(ownerWindow, _viewModel.SelectedIcon);
        if (selectedEmoji != null)
        {
            _viewModel.SelectedIcon = selectedEmoji;
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        _viewModel.ConfirmCommand.Execute(null);

        if (_viewModel.DialogResult)
        {
            Close(_viewModel);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.CancelCommand.Execute(null);
        }
        Close(null);
    }
}


