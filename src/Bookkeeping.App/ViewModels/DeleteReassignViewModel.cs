using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.ViewModels;

public partial class DeleteReassignViewModel : ObservableObject
{
    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private ObservableCollection<CategoriesViewModel.CategoryItemViewModel> _availableTargets = new();

    [ObservableProperty]
    private CategoriesViewModel.CategoryItemViewModel? _selectedTarget;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    private readonly long _categoryToDeleteId;

    public bool CanConfirm => SelectedTarget != null && SelectedTarget.Id != _categoryToDeleteId;

    public DeleteReassignViewModel(Category categoryToDelete, ObservableCollection<CategoriesViewModel.CategoryItemViewModel> sameTypeCategories)
    {
        _categoryToDeleteId = categoryToDelete.Id;
        CategoryName = categoryToDelete.Name;
        TransactionCount = categoryToDelete.Transactions?.Count ?? 0;

        // Filter: same type, exclude the category being deleted
        foreach (var cat in sameTypeCategories.Where(c => c.Id != categoryToDelete.Id))
        {
            AvailableTargets.Add(cat);
        }
    }

    partial void OnSelectedTargetChanged(CategoriesViewModel.CategoryItemViewModel? value)
    {
        OnPropertyChanged(nameof(CanConfirm));
    }

    [RelayCommand]
    private async Task ConfirmAsync(Window ownerWindow)
    {
        if (SelectedTarget == null || SelectedTarget.Id == _categoryToDeleteId)
        {
            ErrorMessage = "请选择一个目标分类";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            await categoryService.ReassignAndDeleteAsync(_categoryToDeleteId, SelectedTarget.Id);

            CloseDialog(ownerWindow, success: true);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "删除分类失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel(Window ownerWindow)
    {
        CloseDialog(ownerWindow, success: false);
    }

    private void CloseDialog(Window ownerWindow, bool success)
    {
        if (ownerWindow != null)
        {
            ownerWindow.Close(success);
        }
    }
}