using System.Collections.ObjectModel;
using System.Linq;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MoneyMap.App.ViewModels.Models;

/// <summary>
/// 字段映射项：展示来源字段到我们字段的对应关系
/// </summary>
public class FieldMappingItem
{
    /// <summary>来源字段名（如"商品说明"）</summary>
    public string SourceField { get; set; } = string.Empty;
    
    /// <summary>我们的字段名（如"Description"）</summary>
    public string TargetField { get; set; } = string.Empty;
    
    /// <summary>我们的字段中文名（如"商品说明"）</summary>
    public string TargetDisplayName { get; set; } = string.Empty;
    
    /// <summary>是否映射成功</summary>
    public bool IsMapped { get; set; }
    
    /// <summary>示例数据（前3条去重值）</summary>
    public string SampleData { get; set; } = string.Empty;
}

/// <summary>
/// 分类映射项：来源分类 → 我们的选择分类
/// </summary>
public class CategoryMappingItem : ObservableObject
{
    /// <summary>来源分类名（如支付宝的"餐饮美食"）</summary>
    public string SourceCategory { get; set; } = string.Empty;
    
    /// <summary>来源分类出现的次数</summary>
    public int Count { get; set; }
    
    /// <summary>推荐的类型（根据来源方向推断）</summary>
    public TransactionType SuggestedType { get; set; }
    
    /// <summary>可选分类列表引用（从 ImportViewModel 传入）</summary>
    public ObservableCollection<Category>? AvailableCategories { get; set; }

    /// <summary>
    /// 按当前 SuggestedType 筛选后的分类列表（ComboBox 绑定用）
    /// </summary>
    public ObservableCollection<Category> FilteredCategories { get; } = new();

    /// <summary>是否勾选（用于批量操作）</summary>
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    /// <summary>
    /// 选中的分类对象（ComboBox 绑定用）
    /// </summary>
    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                TargetCategoryId = value?.Id;
                TargetCategoryName = value?.Name;
            }
        }
    }
    
    /// <summary>选中的目标分类ID</summary>
    private long? _targetCategoryId;
    public long? TargetCategoryId
    {
        get => _targetCategoryId;
        set
        {
            if (SetProperty(ref _targetCategoryId, value))
            {
                OnPropertyChanged(nameof(IsMapped));
            }
        }
    }
    
    /// <summary>选中的目标分类名</summary>
    private string? _targetCategoryName;
    public string? TargetCategoryName
    {
        get => _targetCategoryName;
        set => SetProperty(ref _targetCategoryName, value);
    }
    
    /// <summary>是否已匹配</summary>
    public bool IsMapped => TargetCategoryId.HasValue;

    /// <summary>
    /// 根据 SuggestedType 从 AvailableCategories 中筛选分类，更新 FilteredCategories
    /// </summary>
    public void RefreshFilteredCategories()
    {
        FilteredCategories.Clear();
        if (AvailableCategories == null) return;

        foreach (var cat in AvailableCategories.Where(c => c.Type == SuggestedType))
        {
            FilteredCategories.Add(cat);
        }
    }
}

/// <summary>
/// 支付方式映射项：来源支付方式 → 系统账户
/// </summary>
public class PaymentMethodMappingItem : ObservableObject
{
    /// <summary>来源支付方式名（如"余额"、"花呗"）</summary>
    public string SourcePaymentMethod { get; set; } = string.Empty;
    
    /// <summary>来源支付方式出现的次数</summary>
    public int Count { get; set; }
    
    /// <summary>可选账户列表引用（从 ImportViewModel 传入）</summary>
    public ObservableCollection<Account>? AvailableAccounts { get; set; }
    
    /// <summary>所属分组Key（用于智能分组，如"余额宝"）</summary>
    public string? GroupKey { get; set; }
    
    /// <summary>是否勾选（用于批量操作）</summary>
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    /// <summary>
    /// 选中的账户对象（ComboBox 绑定用）
    /// </summary>
    private Account? _selectedAccount;
    public Account? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
            {
                TargetAccountId = value?.Id;
                TargetAccountName = value?.Name;
            }
        }
    }
    
    /// <summary>选中的目标账户ID</summary>
    private long? _targetAccountId;
    public long? TargetAccountId
    {
        get => _targetAccountId;
        set => SetProperty(ref _targetAccountId, value);
    }
    
    /// <summary>选中的目标账户名</summary>
    private string? _targetAccountName;
    public string? TargetAccountName
    {
        get => _targetAccountName;
        set => SetProperty(ref _targetAccountName, value);
    }
    
    /// <summary>是否已匹配</summary>
    public bool IsMapped => TargetAccountId.HasValue;
}

/// <summary>
/// 类型/状态映射项：展示来源值到标准枚举的映射
/// </summary>
public class TypeStatusMappingItem
{
    /// <summary>映射类型：Type 或 Status</summary>
    public string MappingKind { get; set; } = string.Empty;
    
    /// <summary>来源原始值</summary>
    public string SourceValue { get; set; } = string.Empty;
    
    /// <summary>映射到的标准值中文名</summary>
    public string TargetValue { get; set; } = string.Empty;
    
    /// <summary>映射到的标准枚举值</summary>
    public string TargetEnum { get; set; } = string.Empty;
}

/// <summary>
/// 支付方式分组：将相似名称的支付方式归到同一组
/// 如 "余额宝"、"余额宝&红包"、"余额宝&碰友日立减" 归为 "余额宝" 组
/// </summary>
public class PaymentMethodGroup : ObservableObject
{
    /// <summary>分组名称（如"余额宝"）</summary>
    public string GroupName { get; set; } = string.Empty;
    
/// <summary>分组内所有支付方式项</summary>
    public ObservableCollection<PaymentMethodMappingItem> Items { get; } = new();
    
    /// <summary>分组内项数量</summary>
    public int ItemsCount => Items.Count;
    
    /// <summary>分组总数量</summary>
    public int TotalCount => Items.Sum(i => i.Count);
    
    /// <summary>分组统计信息文本</summary>
    public string GroupSummary => $"共 {ItemsCount} 项 · {TotalCount} 笔";
    
    /// <summary>分组内是否所有项都已映射</summary>
    public bool IsAllMapped => Items.Count > 0 && Items.All(i => i.IsMapped);
    
    /// <summary>分组是否展开（UI 用）</summary>
    private bool _isExpanded = true;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    
    /// <summary>组级别选中的账户（批量映射用）</summary>
    private Account? _groupTargetAccount;
    public Account? GroupTargetAccount
    {
        get => _groupTargetAccount;
        set => SetProperty(ref _groupTargetAccount, value);
    }
}
