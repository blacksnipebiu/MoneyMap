using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    #region Properties

    [ObservableProperty]
    private ObservableCollection<CategoryItemViewModel> _incomeCategories = new();

    [ObservableProperty]
    private ObservableCollection<CategoryItemViewModel> _expenseCategories = new();

    [ObservableProperty]
    private CategoryItemViewModel? _selectedCategory;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private TransactionType _newCategoryType = TransactionType.Expense;

    [ObservableProperty]
    private string? _selectedIcon;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editingName = string.Empty;

    [ObservableProperty]
    private string? _editingIcon;

    /// <summary>
    /// Whether to show the delete-reassign dialog
    /// </summary>
    [ObservableProperty]
    private bool _showDeleteReassignDialog;

    /// <summary>
    /// Category pending deletion (with transactions)
    /// </summary>
    [ObservableProperty]
    private CategoryItemViewModel? _categoryToDelete;

    /// <summary>
    /// Target category for reassignment (when deleting with reassignment)
    /// </summary>
    [ObservableProperty]
    private CategoryItemViewModel? _reassignTargetCategory;

    #endregion

    #region CategoryItemViewModel

    public partial class CategoryItemViewModel : ObservableObject
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public TransactionType Type { get; set; }
        public int SortOrder { get; set; }
        public int TransactionCount { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        public string DisplayName => string.IsNullOrEmpty(Icon) ? Name : $"{Icon} {Name}";
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            var categories = await categoryService.GetAllAsync();
            var categoryList = categories.ToList();

            IncomeCategories.Clear();
            ExpenseCategories.Clear();

            foreach (var category in categoryList.Where(c => c.Type == TransactionType.Income).OrderBy(c => c.SortOrder))
            {
                var vm = new CategoryItemViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Icon = category.Icon,
                    Type = category.Type,
                    SortOrder = category.SortOrder,
                    TransactionCount = category.Transactions?.Count ?? 0
                };
                IncomeCategories.Add(vm);
            }

            foreach (var category in categoryList.Where(c => c.Type == TransactionType.Expense).OrderBy(c => c.SortOrder))
            {
                var vm = new CategoryItemViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Icon = category.Icon,
                    Type = category.Type,
                    SortOrder = category.SortOrder,
                    TransactionCount = category.Transactions?.Count ?? 0
                };
                ExpenseCategories.Add(vm);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "加载分类失败：" + ex.Message;
            App.ToastService.ShowError("加载分类失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            ErrorMessage = "分类名称不能为空";
            App.ToastService.ShowError("分类名称不能为空");
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            var newCategory = new Category
            {
                Name = NewCategoryName.Trim(),
                Icon = SelectedIcon,
                Type = NewCategoryType
            };

            var created = await categoryService.CreateAsync(newCategory);

            var vm = new CategoryItemViewModel
            {
                Id = created.Id,
                Name = created.Name,
                Icon = created.Icon,
                Type = created.Type,
                SortOrder = created.SortOrder,
                TransactionCount = 0
            };

            if (created.Type == TransactionType.Income)
            {
                IncomeCategories.Add(vm);
            }
            else
            {
                ExpenseCategories.Add(vm);
            }

            // Reset form
            NewCategoryName = string.Empty;
            SelectedIcon = null;

            App.ToastService.ShowSuccess($"已添加分类：{created.Name}");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            App.ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "添加分类失败：" + ex.Message;
            App.ToastService.ShowError("添加分类失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditCategory(CategoryItemViewModel? category)
    {
        if (category == null) return;

        SelectedCategory = category;
        EditingName = category.Name;
        EditingIcon = category.Icon;
        IsEditing = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task UpdateCategoryAsync()
    {
        if (SelectedCategory == null) return;

        if (string.IsNullOrWhiteSpace(EditingName))
        {
            ErrorMessage = "分类名称不能为空";
            App.ToastService.ShowError("分类名称不能为空");
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            var categoryToUpdate = await categoryService.GetByIdAsync(SelectedCategory.Id);

            if (categoryToUpdate == null)
            {
                ErrorMessage = "分类不存在";
                App.ToastService.ShowError("分类不存在");
                return;
            }

            categoryToUpdate.Name = EditingName.Trim();
            categoryToUpdate.Icon = EditingIcon;

            await categoryService.UpdateAsync(categoryToUpdate);

            // Update the view model
            SelectedCategory.Name = categoryToUpdate.Name;
            SelectedCategory.Icon = categoryToUpdate.Icon;

            CancelEditCommand.Execute(null);
            App.ToastService.ShowSuccess("分类已更新");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            App.ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "更新分类失败：" + ex.Message;
            App.ToastService.ShowError("更新分类失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(CategoryItemViewModel? category)
    {
        if (category == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();

            // Check if category has transactions - need to check via direct service call
            var categoryToDelete = await categoryService.GetByIdAsync(category.Id);
            if (categoryToDelete == null)
            {
                ErrorMessage = "分类不存在";
                App.ToastService.ShowError("分类不存在");
                return;
            }

            // Get transaction count
            var hasTransactions = categoryToDelete.Transactions?.Count > 0;

            if (hasTransactions)
            {
                // Show reassign dialog
                CategoryToDelete = category;
                ReassignTargetCategory = null;
                ShowDeleteReassignDialog = true;
                IsLoading = false;
                return;
            }

            // Delete directly if no transactions
            await categoryService.DeleteAsync(category.Id);

            // Remove from collection
            if (category.Type == TransactionType.Income)
            {
                IncomeCategories.Remove(category);
            }
            else
            {
                ExpenseCategories.Remove(category);
            }

            App.ToastService.ShowSuccess($"已删除分类：{category.Name}");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            App.ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "删除分类失败：" + ex.Message;
            App.ToastService.ShowError("删除分类失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmDeleteWithReassignAsync()
    {
        if (CategoryToDelete == null || ReassignTargetCategory == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();

            var reassignedCount = await categoryService.ReassignAndDeleteAsync(
                CategoryToDelete.Id,
                ReassignTargetCategory.Id);

            // Remove from collection
            if (CategoryToDelete.Type == TransactionType.Income)
            {
                IncomeCategories.Remove(CategoryToDelete);
            }
            else
            {
                ExpenseCategories.Remove(CategoryToDelete);
            }

            // Update target category's transaction count
            ReassignTargetCategory.TransactionCount += reassignedCount;

            CloseDeleteReassignDialogCommand.Execute(null);
            App.ToastService.ShowSuccess($"已将 {reassignedCount} 笔交易转移到「{ReassignTargetCategory.Name}」，并删除分类");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            App.ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "删除分类失败：" + ex.Message;
            App.ToastService.ShowError("删除分类失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CloseDeleteReassignDialog()
    {
        ShowDeleteReassignDialog = false;
        CategoryToDelete = null;
        ReassignTargetCategory = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        SelectedCategory = null;
        EditingName = string.Empty;
        EditingIcon = null;
        IsEditing = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task MoveUpAsync(CategoryItemViewModel? category)
    {
        if (category == null) return;

        var collection = category.Type == TransactionType.Income ? IncomeCategories : ExpenseCategories;
        var index = collection.IndexOf(category);
        if (index <= 0) return;

        IsLoading = true;
        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            var aboveCategory = collection[index - 1];

            // Swap sort orders
            var tempSortOrder = category.SortOrder;
            var catToUpdate = await categoryService.GetByIdAsync(category.Id);
            var aboveCatToUpdate = await categoryService.GetByIdAsync(aboveCategory.Id);

            if (catToUpdate != null && aboveCatToUpdate != null)
            {
                catToUpdate.SortOrder = aboveCategory.SortOrder;
                aboveCatToUpdate.SortOrder = tempSortOrder;

                await categoryService.UpdateAsync(catToUpdate);
                await categoryService.UpdateAsync(aboveCatToUpdate);

                // Update view models
                category.SortOrder = aboveCategory.SortOrder;
                aboveCategory.SortOrder = tempSortOrder;

                // Reorder in collection
                collection.Move(index, index - 1);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "移动失败：" + ex.Message;
            App.ToastService.ShowError("移动失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MoveDownAsync(CategoryItemViewModel? category)
    {
        if (category == null) return;

        var collection = category.Type == TransactionType.Income ? IncomeCategories : ExpenseCategories;
        var index = collection.IndexOf(category);
        if (index < 0 || index >= collection.Count - 1) return;

        IsLoading = true;
        try
        {
            var categoryService = App.Services.GetRequiredService<ICategoryService>();
            var belowCategory = collection[index + 1];

            // Swap sort orders
            var tempSortOrder = category.SortOrder;
            var catToUpdate = await categoryService.GetByIdAsync(category.Id);
            var belowCatToUpdate = await categoryService.GetByIdAsync(belowCategory.Id);

            if (catToUpdate != null && belowCatToUpdate != null)
            {
                catToUpdate.SortOrder = belowCategory.SortOrder;
                belowCatToUpdate.SortOrder = tempSortOrder;

                await categoryService.UpdateAsync(catToUpdate);
                await categoryService.UpdateAsync(belowCatToUpdate);

                // Update view models
                category.SortOrder = belowCategory.SortOrder;
                belowCategory.SortOrder = tempSortOrder;

                // Reorder in collection
                collection.Move(index, index + 1);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "移动失败：" + ex.Message;
            App.ToastService.ShowError("移动失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}