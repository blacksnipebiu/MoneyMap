using MoneyMap.App.ViewModels.Pages;
using System;
using System.Threading.Tasks;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyMap.App.ViewModels.Dialogs;

public partial class CategoryEditViewModel : ObservableObject
{
    /// <summary>
    /// 默认图标
    /// </summary>
    private const string DefaultIcon = "🏷️";

    private readonly IServiceScopeFactory _scopeFactory;

    #region Properties

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private TransactionType _type = TransactionType.Expense;

    [ObservableProperty]
    private string? _selectedIcon;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private long? _editingCategoryId;

    [ObservableProperty]
    private int _sortOrder;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Dialog result - set to true when confirmed
    /// </summary>
    [ObservableProperty]
    private bool _dialogResult;

    #endregion

    #region Initialization

    public CategoryEditViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void InitializeForAdd(TransactionType defaultType)
    {
        IsEditMode = false;
        EditingCategoryId = null;
        Name = string.Empty;
        Type = defaultType;
        SelectedIcon = DefaultIcon;
        SortOrder = 0;
        ErrorMessage = null;
        DialogResult = false;
    }

    public void InitializeForEdit(CategoriesViewModel.CategoryItemViewModel category)
    {
        IsEditMode = true;
        EditingCategoryId = category.Id;
        Name = category.Name;
        Type = category.Type;
        SelectedIcon = category.Icon;
        SortOrder = category.SortOrder;
        ErrorMessage = null;
        DialogResult = false;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Confirm()
    {
        ErrorMessage = null;

        // Validate non-empty
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "分类名称不能为空";
            return;
        }

        var trimmedName = Name.Trim();

        // Check for duplicate name within same type
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var existingCategories = categoryService.GetByTypeAsync(Type).GetAwaiter().GetResult();

            foreach (var cat in existingCategories)
            {
                // Skip the category being edited
                if (IsEditMode && cat.Id == EditingCategoryId)
                    continue;

                if (string.Equals(cat.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "该类型下已存在同名分类";
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "验证失败：" + ex.Message;
            return;
        }

        // Success
        Name = trimmedName;
        DialogResult = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }

    #endregion
}

