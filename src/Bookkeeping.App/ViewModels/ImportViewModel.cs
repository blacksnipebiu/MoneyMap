using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Bookkeeping.App.Services;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using Bookkeeping.Data.Repositories;
using Bookkeeping.Import;
using Bookkeeping.Import.Detection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Bookkeeping.App.ViewModels;

/// <summary>
/// 筛选选项（带勾选状态）
/// </summary>
public class FilterOption<T> : ObservableObject
{
    public T Value { get; }
    public string DisplayName { get; }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnChanged?.Invoke();
            }
        }
    }

    public Action? OnChanged { get; set; }

    public FilterOption(T value, string displayName, bool isSelected = true)
    {
        Value = value;
        DisplayName = displayName;
        _isSelected = isSelected;
    }
}

/// <summary>
/// 交易记录包装类，支持选择状态
/// </summary>
public class TransactionItem : ObservableObject
{
    public Transaction Transaction { get; }
    
    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public TransactionItem(Transaction transaction)
    {
        Transaction = transaction;
    }
}

public partial class ImportViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string _importStatus = "请拖拽或选择文件导入";

    [ObservableProperty]
    private double _importProgress;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private bool _isDragOver;

    [ObservableProperty]
    private string? _fileName;

    [ObservableProperty]
    private string? _fileSize;

    [ObservableProperty]
    private string? _detectedSource;

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private int _importedCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _importCompleted;

    [ObservableProperty]
    private bool _isParsing;

    [ObservableProperty]
    private bool _hasParsedData;

    /// <summary>
    /// 是否处于字段映射步骤（解析后、预览前）
    /// </summary>
    [ObservableProperty]
    private bool _isMappingStep;

    /// <summary>
    /// 字段映射列表
    /// </summary>
    public ObservableCollection<FieldMappingItem> FieldMappings { get; } = new();

    /// <summary>
    /// 分类映射列表
    /// </summary>
    public ObservableCollection<CategoryMappingItem> CategoryMappings { get; } = new();

    /// <summary>
    /// 类型/状态映射列表
    /// </summary>
    public ObservableCollection<TypeStatusMappingItem> TypeStatusMappings { get; } = new();

    /// <summary>
    /// 支付方式映射列表（来源支付方式 → 系统账户）
    /// </summary>
    public ObservableCollection<PaymentMethodMappingItem> PaymentMethodMappings { get; } = new();

    /// <summary>
    /// 可选分类列表（用于分类映射下拉选择）
    /// </summary>
    public ObservableCollection<Category> AvailableCategories { get; } = new();

    /// <summary>
    /// 是否显示分类映射面板
    /// </summary>
    [ObservableProperty]
    private bool _isShowingCategoryMapping = true;

    /// <summary>
    /// 是否显示字段映射面板
    /// </summary>
    [ObservableProperty]
    private bool _isShowingFieldMapping;

    /// <summary>
    /// 是否显示类型/状态映射面板
    /// </summary>
    [ObservableProperty]
    private bool _isShowingStatusMapping;

    /// <summary>
    /// 是否显示支付方式映射面板
    /// </summary>
    [ObservableProperty]
    private bool _isShowingPaymentMethodMapping;

    /// <summary>
    /// 批量应用分类：选中的目标分类
    /// </summary>
    [ObservableProperty]
    private Category? _batchTargetCategory;

    /// <summary>
    /// 是否有勾选的分类映射（用于批量应用按钮启用状态）
    /// </summary>
    public bool HasSelectedCategoryMappings => CategoryMappings.Any(m => m.IsSelected);

    /// <summary>
    /// 支付方式分组列表
    /// </summary>
    public ObservableCollection<PaymentMethodGroup> PaymentMethodGroups { get; } = new();

    /// <summary>
    /// 所有可用账户
    /// </summary>
    public ObservableCollection<Account> Accounts { get; } = new();

    /// <summary>
    /// 当前选中的账户
    /// </summary>
    [ObservableProperty]
    private Account? _selectedAccount;

    /// <summary>
    /// 是否可以创建新账户
    /// </summary>
    [ObservableProperty]
    private bool _canCreateAccount;

    /// <summary>
    /// 新账户名称（用于创建新账户）
    /// </summary>
    [ObservableProperty]
    private string _newAccountName = string.Empty;

    /// <summary>
    /// 新账户来源
    /// </summary>
    [ObservableProperty]
    private DataSource _newAccountSource;

    /// <summary>
    /// 是否显示创建账户对话框
    /// </summary>
    [ObservableProperty]
    private bool _showCreateAccountDialog;

    /// <summary>
    /// 所有交易数据（带选择状态）- 用于筛选源
    /// </summary>
    private List<TransactionItem> _allTransactions = new();

    /// <summary>
    /// 筛选后显示的交易数据
    /// </summary>
    public ObservableCollection<TransactionItem> FilteredTransactions { get; } = new();

    /// <summary>
    /// 解析结果（临时保存，确认导入时使用）
    /// </summary>
    private ParseResult? _parseResult;

    #region 筛选相关属性

    /// <summary>
    /// 交易类型筛选选项（带勾选状态）
    /// </summary>
    public ObservableCollection<FilterOption<TransactionType>> TypeFilterOptions { get; } = new();

    /// <summary>
    /// 交易状态筛选选项（带勾选状态）
    /// </summary>
    public ObservableCollection<FilterOption<TransactionStatus>> StatusFilterOptions { get; } = new();

    /// <summary>
    /// 支付方式筛选选项（带勾选状态）
    /// </summary>
    public ObservableCollection<FilterOption<string>> PaymentMethodFilterOptions { get; } = new();

    /// <summary>
    /// 类型筛选下拉框是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isTypeFilterOpen;

    /// <summary>
    /// 状态筛选下拉框是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isStatusFilterOpen;

    /// <summary>
    /// 支付方式筛选下拉框是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isPaymentMethodFilterOpen;

    /// <summary>
    /// 类型筛选摘要文本
    /// </summary>
    [ObservableProperty]
    private string _typeFilterSummary = "全部";

    /// <summary>
    /// 状态筛选摘要文本
    /// </summary>
    [ObservableProperty]
    private string _statusFilterSummary = "全部";

    /// <summary>
    /// 支付方式筛选摘要文本
    /// </summary>
    [ObservableProperty]
    private string _paymentMethodFilterSummary = "全部";

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// 选中的记录数
    /// </summary>
    [ObservableProperty]
    private int _selectedCount;

    /// <summary>
    /// 未选中的记录数
    /// </summary>
    [ObservableProperty]
    private int _unselectedCount;

    /// <summary>
    /// 筛选后的总记录数
    /// </summary>
    [ObservableProperty]
    private int _filteredCount;

    #region 分页相关属性

    /// <summary>
    /// 可选的每页数量
    /// </summary>
    public int[] PageSizeOptions { get; } = { 20, 50, 100, 200 };

    /// <summary>
    /// 当前每页数量
    /// </summary>
    [ObservableProperty]
    private int _pageSize = 20;

    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    [ObservableProperty]
    private int _currentPage = 1;

    /// <summary>
    /// 总页数
    /// </summary>
    [ObservableProperty]
    private int _totalPages;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    [ObservableProperty]
    private bool _hasPreviousPage;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    [ObservableProperty]
    private bool _hasNextPage;

    /// <summary>
    /// 筛选后的所有数据（用于分页）
    /// </summary>
    private List<TransactionItem> _filteredData = new();

    #endregion

    #endregion

    /// <summary>
    /// Called from ImportView when a file is selected or dropped.
    /// </summary>
    public void SetFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            ImportStatus = "文件不存在，请重新选择";
            return;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
        {
            ImportStatus = "不支持的文件格式，请选择 CSV 或 Excel 文件";
            return;
        }

        SelectedFilePath = filePath;
        FileName = Path.GetFileName(filePath);
        HasFile = true;
        ImportCompleted = false;
        HasParsedData = false;
        FilteredTransactions.Clear();
        _allTransactions.Clear();
        _parseResult = null;

        var fileInfo = new FileInfo(filePath);
        FileSize = FormatFileSize(fileInfo.Length);

        // Detect source
        try
        {
            var detector = App.Services.GetRequiredService<SourceDetector>();
            using var stream = File.OpenRead(filePath);
            var result = detector.Detect(FileName, stream);
            DetectedSource = result.Source switch
            {
                DataSource.Alipay => "支付宝",
                DataSource.WeChatPay => "微信支付",
                DataSource.BankCard => "银行卡",
                _ => "未知来源"
            };
            ImportStatus = $"已识别来源：{DetectedSource}" + (result.IsConfident ? "" : "（置信度较低，请确认）");
        }
        catch (Exception)
        {
            DetectedSource = null;
            ImportStatus = "已选择文件，但无法自动识别来源";
        }
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        var topLevel = Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var storageProvider = topLevel.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "选择账单文件",
            AllowMultiple = false,
            FileTypeFilter = new System.Collections.Generic.List<Avalonia.Platform.Storage.FilePickerFileType>
            {
                new("账单文件")
                {
                    Patterns = new System.Collections.Generic.List<string> { "*.csv", "*.xlsx", "*.xls" }
                },
                new("所有文件")
                {
                    Patterns = new System.Collections.Generic.List<string> { "*" }
                }
            }
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].Path.LocalPath;
            SetFile(filePath);
        }
    }

    /// <summary>
    /// 解析文件并显示预览
    /// </summary>
    [RelayCommand]
    private async Task ParseFileAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            ImportStatus = "请先选择文件";
            return;
        }

        IsParsing = true;
        ImportStatus = "正在解析文件...";

        try
        {
            var detector = App.Services.GetRequiredService<SourceDetector>();
            var parserFactory = App.Services.GetRequiredService<ParserFactory>();

            await using var stream = File.OpenRead(SelectedFilePath);
            var detection = detector.Detect(FileName!, stream);

            stream.Position = 0;

            IRecordParser? parser = null;
            if (detection.IsConfident)
            {
                parser = parserFactory.GetParser(detection.Source);
            }

            if (parser == null)
            {
                // Try all parsers
                foreach (var source in parserFactory.SupportedSources)
                {
                    var p = parserFactory.GetParser(source);
                    stream.Position = 0;
                    if (p?.CanParse(stream, FileName!) == true)
                    {
                        parser = p;
                        break;
                    }
                }
            }

            if (parser == null)
            {
                ImportStatus = "无法识别文件格式，请确认是否为支付宝或微信账单";
                IsParsing = false;
                return;
            }

            stream.Position = 0;
            _parseResult = parser.Parse(stream, FileName!);

            if (_parseResult.Transactions.Count == 0)
            {
                ImportStatus = _parseResult.Errors.Count > 0
                    ? $"解析失败：{_parseResult.Errors.First().ErrorMessage}"
                    : "未找到有效的交易记录";
                ErrorCount = _parseResult.Errors.Count;
                IsParsing = false;
                return;
            }

            // 存储所有交易数据
            _allTransactions.Clear();
            foreach (var t in _parseResult.Transactions)
            {
                _allTransactions.Add(new TransactionItem(t));
            }

            // 构建字段映射信息
            BuildMappings();

            // 进入映射步骤
            IsMappingStep = true;

            // 加载分类列表（用于分类映射）
            await LoadCategoriesAsync();

            // 加载账户列表
            await LoadAccountsAsync(_parseResult.DetectedSource);
        }
        catch (Exception ex)
        {
            ImportStatus = "解析失败：" + ex.Message;
            App.ToastService.ShowError("解析失败：" + ex.Message);
        }
        finally
        {
            IsParsing = false;
        }
    }

    /// <summary>
    /// 提取筛选选项
    /// </summary>
    private void ExtractFilterOptions()
    {
        // 初始化类型筛选选项
        TypeFilterOptions.Clear();
        TypeFilterOptions.Add(new FilterOption<TransactionType>(TransactionType.Expense, "支出") { OnChanged = OnFilterOptionChanged });
        TypeFilterOptions.Add(new FilterOption<TransactionType>(TransactionType.Income, "收入") { OnChanged = OnFilterOptionChanged });
        TypeFilterOptions.Add(new FilterOption<TransactionType>(TransactionType.Transfer, "转账") { OnChanged = OnFilterOptionChanged });

        // 提取状态选项（使用 TransactionStatus 枚举）
        StatusFilterOptions.Clear();
        var statuses = _allTransactions
            .Select(t => t.Transaction.Status)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        foreach (var status in statuses)
        {
            var displayName = status switch
            {
                TransactionStatus.Completed => "已完成",
                TransactionStatus.Refunded => "已退款",
                TransactionStatus.Pending => "待处理",
                TransactionStatus.Cancelled => "已关闭",
                TransactionStatus.Other => "其他",
                _ => status.ToString()
            };
            StatusFilterOptions.Add(new FilterOption<TransactionStatus>(status, displayName) { OnChanged = OnFilterOptionChanged });
        }

        // 提取支付方式选项
        PaymentMethodFilterOptions.Clear();
        var paymentMethods = _allTransactions
            .Select(t => t.Transaction.PaymentMethod)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        foreach (var method in paymentMethods)
        {
            PaymentMethodFilterOptions.Add(new FilterOption<string>(method!, method!) { OnChanged = OnFilterOptionChanged });
        }
    }

    /// <summary>
    /// 构建字段映射信息
    /// </summary>
    private void BuildMappings()
    {
        FieldMappings.Clear();
        CategoryMappings.Clear();
        TypeStatusMappings.Clear();

        if (_parseResult == null || _allTransactions.Count == 0) return;

        var source = _parseResult.DetectedSource;

        // 字段映射
        var fieldMap = source switch
        {
            DataSource.Alipay => new List<FieldMappingItem>
            {
                new() { SourceField = "交易时间", TargetField = "TransactionTime", TargetDisplayName = "交易时间", IsMapped = true },
                new() { SourceField = "交易分类", TargetField = "CategoryName", TargetDisplayName = "分类", IsMapped = true },
                new() { SourceField = "交易对方", TargetField = "Counterparty", TargetDisplayName = "交易对方", IsMapped = true },
                new() { SourceField = "对方账号", TargetField = "CounterpartyAccount", TargetDisplayName = "对方账号", IsMapped = true },
                new() { SourceField = "商品说明", TargetField = "Description", TargetDisplayName = "说明", IsMapped = true },
                new() { SourceField = "收/支", TargetField = "Type", TargetDisplayName = "类型", IsMapped = true },
                new() { SourceField = "金额", TargetField = "Amount", TargetDisplayName = "金额", IsMapped = true },
                new() { SourceField = "收/付款方式", TargetField = "PaymentMethod", TargetDisplayName = "支付方式", IsMapped = true },
                new() { SourceField = "交易状态", TargetField = "Status", TargetDisplayName = "状态", IsMapped = true },
                new() { SourceField = "来源平台", TargetField = "DataSourceName", TargetDisplayName = "数据来源", IsMapped = true },
                new() { SourceField = "交易订单号", TargetField = "SourceTransactionId", TargetDisplayName = "来源订单号", IsMapped = true },
                new() { SourceField = "商家订单号", TargetField = "MerchantOrderId", TargetDisplayName = "商家订单号", IsMapped = true },
                new() { SourceField = "备注", TargetField = "Remark", TargetDisplayName = "备注", IsMapped = true },
            },
            DataSource.WeChatPay => new List<FieldMappingItem>
            {
                new() { SourceField = "交易时间", TargetField = "TransactionTime", TargetDisplayName = "交易时间", IsMapped = true },
                new() { SourceField = "交易类型", TargetField = "CategoryName", TargetDisplayName = "分类", IsMapped = true },
                new() { SourceField = "交易对方", TargetField = "Counterparty", TargetDisplayName = "交易对方", IsMapped = true },
                new() { SourceField = "商品", TargetField = "Description", TargetDisplayName = "说明", IsMapped = true },
                new() { SourceField = "收/支", TargetField = "Type", TargetDisplayName = "类型", IsMapped = true },
                new() { SourceField = "金额(元)", TargetField = "Amount", TargetDisplayName = "金额", IsMapped = true },
                new() { SourceField = "支付方式", TargetField = "PaymentMethod", TargetDisplayName = "支付方式", IsMapped = true },
                new() { SourceField = "当前状态", TargetField = "Status", TargetDisplayName = "状态", IsMapped = true },
                new() { SourceField = "来源平台", TargetField = "DataSourceName", TargetDisplayName = "数据来源", IsMapped = true },
                new() { SourceField = "交易单号", TargetField = "SourceTransactionId", TargetDisplayName = "来源订单号", IsMapped = true },
                new() { SourceField = "商户单号", TargetField = "MerchantOrderId", TargetDisplayName = "商家订单号", IsMapped = true },
                new() { SourceField = "备注", TargetField = "Remark", TargetDisplayName = "备注", IsMapped = true },
            },
            _ => new List<FieldMappingItem>()
        };

        // 填充示例数据
        foreach (var item in fieldMap)
        {
            var samples = _allTransactions
                .Select(t => GetFieldValue(t.Transaction, item.TargetField))
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .Take(3)
                .ToList();
            item.SampleData = samples.Count > 0 ? string.Join("、", samples) : "（无数据）";
            FieldMappings.Add(item);
        }

        // 类型映射
        var typeMappings = _allTransactions
            .Select(t => t.Transaction.Type)
            .Distinct()
            .Select(t => new TypeStatusMappingItem
            {
                MappingKind = "类型",
                SourceValue = t switch
                {
                    TransactionType.Expense => "支出",
                    TransactionType.Income => "收入",
                    TransactionType.Transfer => "转账",
                    _ => t.ToString()
                },
                TargetValue = t switch
                {
                    TransactionType.Expense => "支出",
                    TransactionType.Income => "收入",
                    TransactionType.Transfer => "转账",
                    _ => "其他"
                },
                TargetEnum = t.ToString()
            })
            .ToList();
        foreach (var m in typeMappings) TypeStatusMappings.Add(m);

        // 状态映射
        var statusMappings = _allTransactions
            .Select(t => new { t.Transaction.Status, t.Transaction.RawStatus })
            .DistinctBy(x => x.Status)
            .Select(x => new TypeStatusMappingItem
            {
                MappingKind = "状态",
                SourceValue = x.RawStatus ?? x.Status.ToString(),
                TargetValue = x.Status switch
                {
                    TransactionStatus.Completed => "已完成",
                    TransactionStatus.Refunded => "已退款",
                    TransactionStatus.Pending => "待处理",
                    TransactionStatus.Cancelled => "已关闭",
                    TransactionStatus.Other => "其他",
                    _ => x.Status.ToString()
                },
                TargetEnum = x.Status.ToString()
            })
            .ToList();
        foreach (var m in statusMappings) TypeStatusMappings.Add(m);

        // 分类映射
        BuildCategoryMappings();

        // 支付方式映射
        BuildPaymentMethodMappings();
    }

    /// <summary>
    /// 构建支付方式映射（来源支付方式 → 系统账户）
    /// 使用智能分组将相似名称归到同一组
    /// </summary>
    private void BuildPaymentMethodMappings()
    {
        PaymentMethodMappings.Clear();
        PaymentMethodGroups.Clear();

        var methodGroups = _allTransactions
            .Where(t => !string.IsNullOrEmpty(t.Transaction.PaymentMethod))
            .GroupBy(t => t.Transaction.PaymentMethod!)
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var group in methodGroups)
        {
            var item = new PaymentMethodMappingItem
            {
                SourcePaymentMethod = group.Key,
                Count = group.Count(),
                AvailableAccounts = Accounts
            };

            // 尝试自动匹配账户（根据来源匹配）
            var matchingAccount = Accounts.FirstOrDefault(a =>
                item.SourcePaymentMethod.Contains(a.Source switch
                {
                    DataSource.Alipay => "支付宝",
                    DataSource.WeChatPay => "微信",
                    DataSource.BankCard => "银行",
                    _ => ""
                }) ||
                a.Name.Contains(item.SourcePaymentMethod));

            if (matchingAccount != null)
            {
                item.TargetAccountId = matchingAccount.Id;
                item.TargetAccountName = matchingAccount.Name;
                item.SelectedAccount = matchingAccount;
            }

            PaymentMethodMappings.Add(item);
        }

        // 智能分组：将名称相似的支付方式归到同一组
        BuildPaymentMethodGroups();
    }

    /// <summary>
    /// 智能分组支付方式：提取共同前缀作为组名
    /// 如 "余额宝"、"余额宝&红包"、"余额宝&碰友日立减" 归为 "余额宝" 组
    /// </summary>
    private void BuildPaymentMethodGroups()
    {
        PaymentMethodGroups.Clear();

        var items = PaymentMethodMappings.ToList();
        var assigned = new HashSet<PaymentMethodMappingItem>();
        var groups = new List<PaymentMethodGroup>();

        // 按出现次数降序处理，确保最常见的名称先成组
        foreach (var item in items.OrderByDescending(i => i.Count))
        {
            if (assigned.Contains(item)) continue;

            var groupName = ExtractPaymentMethodRoot(item.SourcePaymentMethod);
            var group = new PaymentMethodGroup { GroupName = groupName };

            // 找到所有属于同一组的项
            var groupItems = items.Where(i =>
                !assigned.Contains(i) &&
                ExtractPaymentMethodRoot(i.SourcePaymentMethod) == groupName).ToList();

            foreach (var gi in groupItems)
            {
                gi.GroupKey = groupName;
                group.Items.Add(gi);
                assigned.Add(gi);
            }

            // 设置 Accounts 引用用于 ComboBox 绑定
            group.GroupTargetAccount = Accounts.FirstOrDefault();

            groups.Add(group);
        }

        foreach (var g in groups)
        {
            PaymentMethodGroups.Add(g);
        }
    }

    /// <summary>
    /// 提取支付方式名称的根名（去除后缀变体）
    /// 规则：取 "&" 或 "(" 之前的部分，或最后一个常见后缀之前的部分
    /// </summary>
    private static string ExtractPaymentMethodRoot(string name)
    {
        // 规则1: 以 "&" 分隔（如 "余额宝&红包" → "余额宝"）
        if (name.Contains('&'))
            return name.Split('&')[0].Trim();

        // 规则2: 以 "（" 或 "(" 分隔（如 "招商银行(信用卡)" → "招商银行"）
        if (name.Contains('（'))
            return name.Split('（')[0].Trim();
        if (name.Contains('('))
            return name.Split('(')[0].Trim();

        // 规则3: 以 "-" 分隔（如 "花呗-分期" → "花呗"）
        if (name.Contains('-'))
            return name.Split('-')[0].Trim();

        // 无特殊分隔符，返回原名
        return name;
    }

    private static string? GetFieldValue(Transaction t, string fieldName)
    {
        return fieldName switch
        {
            "TransactionTime" => t.TransactionTime.ToString("yyyy-MM-dd"),
            "CategoryName" => t.CategoryName,
            "Counterparty" => t.Counterparty,
            "CounterpartyAccount" => t.CounterpartyAccount,
            "Description" => t.Description,
            "Type" => t.Type.ToString(),
            "Amount" => t.Amount.ToString("F2"),
            "PaymentMethod" => t.PaymentMethod,
            "Status" => t.Status.ToString(),
            "SourceTransactionId" => t.SourceTransactionId,
            "MerchantOrderId" => t.MerchantOrderId,
            "Remark" => t.Remark,
            _ => null
        };
    }

    /// <summary>
    /// 构建分类映射（来源分类 → 我们的分类）
    /// </summary>
    private void BuildCategoryMappings()
    {
        CategoryMappings.Clear();

        var categoryGroups = _allTransactions
            .GroupBy(t => new { t.Transaction.CategoryName, t.Transaction.Type })
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var group in categoryGroups)
        {
            var cat = group.Key.CategoryName;
            if (string.IsNullOrEmpty(cat) || cat == "/") continue;

            var item = new CategoryMappingItem
            {
                SourceCategory = cat,
                Count = group.Count(),
                SuggestedType = group.Key.Type,
                AvailableCategories = AvailableCategories
            };

            // 尝试自动匹配分类
            AutoMatchCategory(item);

            // 监听勾选状态变化以更新批量按钮状态
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CategoryMappingItem.IsSelected))
                    OnPropertyChanged(nameof(HasSelectedCategoryMappings));
            };

            CategoryMappings.Add(item);
        }
    }

    /// <summary>
    /// 自动匹配分类
    /// </summary>
    private void AutoMatchCategory(CategoryMappingItem item)
    {
        var category = AvailableCategories.FirstOrDefault(c =>
            string.Equals(c.Name, item.SourceCategory, StringComparison.OrdinalIgnoreCase) ||
            (c.SourceCategoryMapping != null &&
             c.SourceCategoryMapping.Split(',', ';').Contains(item.SourceCategory, StringComparer.OrdinalIgnoreCase)) ||
            (c.AutoMatchPattern != null &&
             System.Text.RegularExpressions.Regex.IsMatch(item.SourceCategory, c.AutoMatchPattern)));

        if (category != null)
        {
            item.TargetCategoryId = category.Id;
            item.TargetCategoryName = category.Name;
            item.SelectedCategory = category;
        }
    }

    /// <summary>
    /// 加载分类列表
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        using var scope = App.Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var categories = await categoryRepo.GetAllAsync();

        AvailableCategories.Clear();
        foreach (var category in categories)
        {
            AvailableCategories.Add(category);
        }

        // 自动匹配分类
        foreach (var item in CategoryMappings)
        {
            if (!item.IsMapped)
            {
                AutoMatchCategory(item);
            }
        }
    }

    /// <summary>
    /// 确认映射并进入预览
    /// </summary>
    [RelayCommand]
    private void ConfirmMapping()
    {
        var defaultAccount = SelectedAccount;

        // 应用分类映射到交易数据
        foreach (var mapping in CategoryMappings.Where(m => m.IsMapped))
        {
            var transactions = _allTransactions
                .Where(t => t.Transaction.CategoryName == mapping.SourceCategory)
                .ToList();

            foreach (var t in transactions)
            {
                t.Transaction.CategoryId = mapping.TargetCategoryId;
                t.Transaction.CategoryName = mapping.TargetCategoryName;
            }
        }

        // 应用支付方式映射到交易数据（AccountId + DataSourceName）
        foreach (var mapping in PaymentMethodMappings.Where(m => m.IsMapped))
        {
            var transactions = _allTransactions
                .Where(t => t.Transaction.PaymentMethod == mapping.SourcePaymentMethod)
                .ToList();

            foreach (var t in transactions)
            {
                t.Transaction.AccountId = mapping.TargetAccountId!.Value;
                t.Transaction.DataSourceName = mapping.TargetAccountName;
            }
        }

        // 未映射支付方式的交易，使用默认账户
        if (defaultAccount != null)
        {
            foreach (var t in _allTransactions.Where(t => t.Transaction.AccountId == 0))
            {
                t.Transaction.AccountId = defaultAccount.Id;
                t.Transaction.DataSourceName = defaultAccount.Name;
            }
        }

        // 提取筛选选项
        ExtractFilterOptions();

        // 重置筛选条件
        SearchKeyword = string.Empty;

        // 应用筛选并显示
        ApplyFilter();

        IsMappingStep = false;
        HasParsedData = true;
        ImportedCount = _allTransactions.Count;
        SkippedCount = _parseResult?.SkippedCount ?? 0;
        ErrorCount = _parseResult?.Errors.Count ?? 0;

        ImportStatus = $"解析完成：共 {ImportedCount} 条交易记录" +
                      (SkippedCount > 0 ? $"，跳过 {SkippedCount} 条" : "") +
                      (ErrorCount > 0 ? $"，错误 {ErrorCount} 条" : "");
    }

    /// <summary>
    /// 一键为未匹配的分类创建新分类
    /// </summary>
    [RelayCommand]
    private async Task CreateCategoryFromMappingAsync(CategoryMappingItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.SourceCategory)) return;

        var newCategory = new Category
        {
            Name = item.SourceCategory,
            Type = item.SuggestedType,
            SourceCategoryMapping = item.SourceCategory,
            SortOrder = 99
        };

        using var scope = App.Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        await categoryRepo.AddAsync(newCategory);

        // 更新可选列表
        AvailableCategories.Add(newCategory);

        // 更新映射
        item.TargetCategoryId = newCategory.Id;
        item.TargetCategoryName = newCategory.Name;
        item.SelectedCategory = newCategory;

        App.ToastService.ShowSuccess($"已创建分类：{newCategory.Name}");
    }

    /// <summary>
    /// 批量应用分类：将选中的目标分类应用到所有勾选的未匹配映射项
    /// </summary>
    [RelayCommand]
    private void BatchApplyCategory()
    {
        if (BatchTargetCategory == null) return;

        var selectedItems = CategoryMappings.Where(m => m.IsSelected && !m.IsMapped).ToList();
        if (selectedItems.Count == 0)
        {
            App.ToastService.ShowWarning("请先勾选需要批量映射的分类");
            return;
        }

        foreach (var item in selectedItems)
        {
            item.TargetCategoryId = BatchTargetCategory.Id;
            item.TargetCategoryName = BatchTargetCategory.Name;
            item.SelectedCategory = BatchTargetCategory;
            item.IsSelected = false; // 应用后取消勾选
        }

        App.ToastService.ShowSuccess($"已将 {selectedItems.Count} 个分类映射到「{BatchTargetCategory.Name}」");
        OnPropertyChanged(nameof(HasSelectedCategoryMappings));
    }

    /// <summary>
    /// 全选/取消全选未匹配的分类映射项
    /// </summary>
    [RelayCommand]
    private void ToggleSelectAllCategoryMappings()
    {
        var unmappedItems = CategoryMappings.Where(m => !m.IsMapped).ToList();
        if (unmappedItems.Count == 0) return;

        var allSelected = unmappedItems.All(m => m.IsSelected);
        foreach (var item in unmappedItems)
        {
            item.IsSelected = !allSelected;
        }
        OnPropertyChanged(nameof(HasSelectedCategoryMappings));
    }

    /// <summary>
    /// 一键为所有未匹配分类创建新分类
    /// </summary>
    [RelayCommand]
    private async Task BatchCreateAllCategoriesAsync()
    {
        var unmappedItems = CategoryMappings.Where(m => !m.IsMapped).ToList();
        if (unmappedItems.Count == 0)
        {
            App.ToastService.ShowWarning("所有分类已映射，无需新建");
            return;
        }

        using var scope = App.Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var created = 0;
        foreach (var item in unmappedItems)
        {
            var newCategory = new Category
            {
                Name = item.SourceCategory,
                Type = item.SuggestedType,
                SourceCategoryMapping = item.SourceCategory,
                SortOrder = 99
            };

            await categoryRepo.AddAsync(newCategory);
            AvailableCategories.Add(newCategory);

            item.TargetCategoryId = newCategory.Id;
            item.TargetCategoryName = newCategory.Name;
            item.SelectedCategory = newCategory;
            created++;
        }

        App.ToastService.ShowSuccess($"已创建 {created} 个分类");
    }

    /// <summary>
    /// 批量映射支付方式分组：将组内所有未匹配项映射到选中的账户
    /// </summary>
    [RelayCommand]
    private void BatchApplyPaymentMethodGroup(PaymentMethodGroup? group)
    {
        if (group == null || group.GroupTargetAccount == null) return;

        var unmappedItems = group.Items.Where(i => !i.IsMapped).ToList();
        foreach (var item in unmappedItems)
        {
            item.TargetAccountId = group.GroupTargetAccount.Id;
            item.TargetAccountName = group.GroupTargetAccount.Name;
            item.SelectedAccount = group.GroupTargetAccount;
        }

        App.ToastService.ShowSuccess($"已将 {unmappedItems.Count} 个支付方式映射到「{group.GroupTargetAccount.Name}」");
    }

    /// <summary>
    /// 设置分类映射（从 UI 调用）
    /// </summary>
    public void SetCategoryMapping(CategoryMappingItem item, Category? selectedCategory)
    {
        if (selectedCategory != null)
        {
            item.TargetCategoryId = selectedCategory.Id;
            item.TargetCategoryName = selectedCategory.Name;
        }
        else
        {
            item.TargetCategoryId = null;
            item.TargetCategoryName = null;
        }
    }

    [RelayCommand]
    private void ShowCategoryMapping()
    {
        IsShowingCategoryMapping = true;
        IsShowingFieldMapping = false;
        IsShowingStatusMapping = false;
    }

    [RelayCommand]
    private void ShowFieldMapping()
    {
        IsShowingCategoryMapping = false;
        IsShowingFieldMapping = true;
        IsShowingStatusMapping = false;
    }

    [RelayCommand]
    private void ShowStatusMapping()
    {
        IsShowingCategoryMapping = false;
        IsShowingFieldMapping = false;
        IsShowingStatusMapping = true;
        IsShowingPaymentMethodMapping = false;
    }

    [RelayCommand]
    private void ShowPaymentMethodMapping()
    {
        IsShowingCategoryMapping = false;
        IsShowingFieldMapping = false;
        IsShowingStatusMapping = false;
        IsShowingPaymentMethodMapping = true;
    }

    /// <summary>
    /// 应用筛选条件并更新显示
    /// </summary>
    partial void OnSearchKeywordChanged(string value) => ApplyFilter();
    partial void OnPageSizeChanged(int value) => ApplyPagination();
    partial void OnCurrentPageChanged(int value) => ApplyPagination();

    /// <summary>
    /// 应用筛选（当筛选选项变化时调用）
    /// </summary>
    public void OnFilterOptionChanged()
    {
        ApplyFilter();
    }

    /// <summary>
    /// 应用筛选
    /// </summary>
    private void ApplyFilter()
    {
        if (_allTransactions.Count == 0) return;

        var filtered = _allTransactions.AsEnumerable();

        // 按类型筛选（勾选的类型）
        var selectedTypes = TypeFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToList();
        if (selectedTypes.Count > 0 && selectedTypes.Count < TypeFilterOptions.Count)
        {
            filtered = filtered.Where(t => selectedTypes.Contains(t.Transaction.Type));
        }

        // 按状态筛选（勾选的状态）
        var selectedStatuses = StatusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToList();
        if (selectedStatuses.Count > 0 && selectedStatuses.Count < StatusFilterOptions.Count)
        {
            filtered = filtered.Where(t => selectedStatuses.Contains(t.Transaction.Status));
        }

        // 按支付方式筛选（勾选的方式）
        var selectedPaymentMethods = PaymentMethodFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToList();
        if (selectedPaymentMethods.Count > 0 && selectedPaymentMethods.Count < PaymentMethodFilterOptions.Count)
        {
            filtered = filtered.Where(t => !string.IsNullOrEmpty(t.Transaction.PaymentMethod) && selectedPaymentMethods.Contains(t.Transaction.PaymentMethod));
        }

        // 按关键词搜索
        if (!string.IsNullOrEmpty(SearchKeyword))
        {
            var keyword = SearchKeyword.ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Transaction.Counterparty?.ToLowerInvariant().Contains(keyword) ?? false) ||
                (t.Transaction.Description?.ToLowerInvariant().Contains(keyword) ?? false) ||
                (t.Transaction.CategoryName?.ToLowerInvariant().Contains(keyword) ?? false));
        }

        // 保存筛选后的数据
        _filteredData = filtered.ToList();

        // 更新统计
        FilteredCount = _filteredData.Count;
        SelectedCount = _filteredData.Count(t => t.IsSelected);
        UnselectedCount = _filteredData.Count(t => !t.IsSelected);

        // 更新筛选摘要
        UpdateFilterSummaries();

        // 重置页码并应用分页
        CurrentPage = 1;
        ApplyPagination();
    }

    /// <summary>
    /// 更新筛选摘要文本
    /// </summary>
    private void UpdateFilterSummaries()
    {
        // 类型筛选摘要
        var selectedTypes = TypeFilterOptions.Where(o => o.IsSelected).ToList();
        if (selectedTypes.Count == 0)
        {
            TypeFilterSummary = "无";
        }
        else if (selectedTypes.Count == TypeFilterOptions.Count)
        {
            TypeFilterSummary = "全部";
        }
        else
        {
            TypeFilterSummary = string.Join(", ", selectedTypes.Select(o => o.DisplayName));
        }

        // 状态筛选摘要
        var selectedStatuses = StatusFilterOptions.Where(o => o.IsSelected).ToList();
        if (selectedStatuses.Count == 0)
        {
            StatusFilterSummary = "无";
        }
        else if (selectedStatuses.Count == StatusFilterOptions.Count)
        {
            StatusFilterSummary = "全部";
        }
        else if (selectedStatuses.Count <= 3)
        {
            StatusFilterSummary = string.Join(", ", selectedStatuses.Select(o => o.DisplayName));
        }
        else
        {
            StatusFilterSummary = $"{selectedStatuses.Count} 项";
        }

        // 支付方式筛选摘要
        var selectedMethods = PaymentMethodFilterOptions.Where(o => o.IsSelected).ToList();
        if (selectedMethods.Count == 0)
        {
            PaymentMethodFilterSummary = "无";
        }
        else if (selectedMethods.Count == PaymentMethodFilterOptions.Count)
        {
            PaymentMethodFilterSummary = "全部";
        }
        else if (selectedMethods.Count <= 3)
        {
            PaymentMethodFilterSummary = string.Join(", ", selectedMethods.Select(o => o.DisplayName));
        }
        else
        {
            PaymentMethodFilterSummary = $"{selectedMethods.Count} 项";
        }
    }

    /// <summary>
    /// 应用分页
    /// </summary>
    private void ApplyPagination()
    {
        if (_filteredData.Count == 0)
        {
            FilteredTransactions.Clear();
            TotalPages = 0;
            HasPreviousPage = false;
            HasNextPage = false;
            return;
        }

        // 计算总页数
        TotalPages = (int)Math.Ceiling(_filteredData.Count / (double)PageSize);

        // 确保当前页码有效
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        // 更新分页状态
        HasPreviousPage = CurrentPage > 1;
        HasNextPage = CurrentPage < TotalPages;

        // 获取当前页数据
        var startIndex = (CurrentPage - 1) * PageSize;
        var pageData = _filteredData.Skip(startIndex).Take(PageSize).ToList();

        // 更新显示列表
        FilteredTransactions.Clear();
        foreach (var item in pageData)
        {
            FilteredTransactions.Add(item);
        }
    }

    /// <summary>
    /// 上一页
    /// </summary>
    [RelayCommand]
    private void PreviousPage()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
        }
    }

    /// <summary>
    /// 下一页
    /// </summary>
    [RelayCommand]
    private void NextPage()
    {
        if (HasNextPage)
        {
            CurrentPage++;
        }
    }

    /// <summary>
    /// 跳转到第一页
    /// </summary>
    [RelayCommand]
    private void FirstPage()
    {
        CurrentPage = 1;
    }

    /// <summary>
    /// 跳转到最后一页
    /// </summary>
    [RelayCommand]
    private void LastPage()
    {
        CurrentPage = TotalPages;
    }

    /// <summary>
    /// 更新选中计数
    /// </summary>
    private void UpdateSelectionCounts()
    {
        SelectedCount = _filteredData.Count(t => t.IsSelected);
        UnselectedCount = _filteredData.Count(t => !t.IsSelected);
    }

    /// <summary>
    /// 全选筛选后的数据（所有页）
    /// </summary>
    [RelayCommand]
    private void SelectAllFiltered()
    {
        foreach (var item in _filteredData)
        {
            item.IsSelected = true;
        }
        UpdateSelectionCounts();
    }

    /// <summary>
    /// 取消选择筛选后的数据（所有页）
    /// </summary>
    [RelayCommand]
    private void DeselectAllFiltered()
    {
        foreach (var item in _filteredData)
        {
            item.IsSelected = false;
        }
        UpdateSelectionCounts();
    }

    /// <summary>
    /// 反选筛选后的数据（所有页）
    /// </summary>
    [RelayCommand]
    private void InvertSelectionFiltered()
    {
        foreach (var item in _filteredData)
        {
            item.IsSelected = !item.IsSelected;
        }
        UpdateSelectionCounts();
    }

    /// <summary>
    /// 清除所有筛选条件（全选所有筛选选项）
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        // 全选所有类型
        foreach (var option in TypeFilterOptions)
        {
            option.IsSelected = true;
        }
        // 全选所有状态
        foreach (var option in StatusFilterOptions)
        {
            option.IsSelected = true;
        }
        // 全选所有支付方式
        foreach (var option in PaymentMethodFilterOptions)
        {
            option.IsSelected = true;
        }
        SearchKeyword = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// 加载账户列表
    /// </summary>
    private async Task LoadAccountsAsync(DataSource detectedSource)
    {
        using var scope = App.Services.CreateScope();
        var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var accounts = await accountRepo.GetAllAsync();

        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(account);
        }

        // 自动选择匹配的账户
        SelectedAccount = Accounts.FirstOrDefault(a => a.Source == detectedSource);

        // 如果没有匹配的账户，提示用户创建
        if (SelectedAccount == null)
        {
            NewAccountSource = detectedSource;
            NewAccountName = detectedSource switch
            {
                DataSource.Alipay => "支付宝",
                DataSource.WeChatPay => "微信支付",
                DataSource.BankCard => "银行卡",
                _ => "新账户"
            };
            CanCreateAccount = true;
        }
    }

    /// <summary>
    /// 打开创建账户对话框
    /// </summary>
    [RelayCommand]
    private void OpenCreateAccountDialog()
    {
        ShowCreateAccountDialog = true;
    }

    /// <summary>
    /// 创建新账户
    /// </summary>
    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            App.ToastService.ShowError("请输入账户名称");
            return;
        }

        using var scope = App.Services.CreateScope();
        var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        var newAccount = new Account
        {
            Name = NewAccountName,
            Source = NewAccountSource,
            CreatedAt = DateTime.Now
        };

        await accountRepo.AddAsync(newAccount);
        Accounts.Add(newAccount);
        SelectedAccount = newAccount;
        ShowCreateAccountDialog = false;

        App.ToastService.ShowSuccess($"已创建账户：{NewAccountName}");
    }

    /// <summary>
    /// 取消创建账户
    /// </summary>
    [RelayCommand]
    private void CancelCreateAccount()
    {
        ShowCreateAccountDialog = false;
    }

    /// <summary>
    /// 确认导入（仅导入选中的记录）
    /// </summary>
    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (_parseResult == null || _allTransactions.Count == 0)
        {
            ImportStatus = "没有可导入的数据";
            return;
        }

        // 获取选中的交易
        var selectedTransactions = _allTransactions
            .Where(t => t.IsSelected)
            .Select(t => t.Transaction)
            .ToList();

        if (selectedTransactions.Count == 0)
        {
            App.ToastService.ShowWarning("请至少选择一条记录进行导入");
            return;
        }

        if (SelectedAccount == null)
        {
            App.ToastService.ShowError("请选择或创建一个账户");
            return;
        }

        IsImporting = true;
        ImportProgress = 0;
        ImportStatus = "正在保存到数据库...";

        try
        {
            using var scope = App.Services.CreateScope();
            var transactionRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
            var importRecordRepo = scope.ServiceProvider.GetRequiredService<IImportRecordRepository>();

            // Deduplicate by SourceTransactionId
            var newTransactions = selectedTransactions
                .Where(t => string.IsNullOrEmpty(t.SourceTransactionId) ||
                            !transactionRepo.ExistsBySourceTransactionIdAsync(t.SourceTransactionId).GetAwaiter().GetResult())
                .ToList();

            var duplicateCount = selectedTransactions.Count - newTransactions.Count;
            var excludedCount = _allTransactions.Count - selectedTransactions.Count;

            if (newTransactions.Count > 0)
            {
                // Create import record
                var importRecord = new ImportRecord
                {
                    FileName = FileName!,
                    Source = _parseResult.DetectedSource,
                    ImportTime = DateTime.Now,
                    TotalRows = _parseResult.TotalRows,
                    ImportedCount = newTransactions.Count,
                    SkippedCount = _parseResult.SkippedCount + duplicateCount + excludedCount,
                    ErrorCount = _parseResult.Errors.Count
                };

                // Assign import record and account to each transaction
                foreach (var t in newTransactions)
                {
                    t.ImportRecord = importRecord;
                    t.AccountId = SelectedAccount.Id;
                }

                await transactionRepo.AddRangeAsync(newTransactions);
                ImportProgress = 90;
            }

            ImportedCount = newTransactions.Count;
            SkippedCount = _parseResult.SkippedCount + duplicateCount;
            ErrorCount = _parseResult.Errors.Count;
            ImportProgress = 100;
            ImportCompleted = true;
            HasParsedData = false;

            var statusParts = new System.Collections.Generic.List<string>();
            if (ImportedCount > 0) statusParts.Add($"成功导入 {ImportedCount} 条");
            if (excludedCount > 0) statusParts.Add($"用户排除 {excludedCount} 条");
            if (SkippedCount > 0) statusParts.Add($"跳过 {SkippedCount} 条");
            if (duplicateCount > 0) statusParts.Add($"重复 {duplicateCount} 条");
            if (ErrorCount > 0) statusParts.Add($"错误 {ErrorCount} 条");

            ImportStatus = "导入完成！" + string.Join("，", statusParts);

            if (ImportedCount > 0)
            {
                App.ToastService.ShowSuccess("成功导入 " + ImportedCount + " 条交易记录");
            }
            else
            {
                App.ToastService.ShowWarning("没有新记录被导入");
            }
        }
        catch (Exception ex)
        {
            ImportStatus = "导入失败：" + ex.Message;
            App.ToastService.ShowError("导入失败：" + ex.Message);
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private void ClearFile()
    {
        SelectedFilePath = null;
        FileName = null;
        FileSize = null;
        DetectedSource = null;
        HasFile = false;
        ImportCompleted = false;
        HasParsedData = false;
        IsMappingStep = false;
        ImportProgress = 0;
        ImportedCount = 0;
        SkippedCount = 0;
        ErrorCount = 0;
        FilteredTransactions.Clear();
        _allTransactions.Clear();
        _filteredData.Clear();
        _parseResult = null;
        SelectedAccount = null;
        TypeFilterOptions.Clear();
        StatusFilterOptions.Clear();
        PaymentMethodFilterOptions.Clear();
        SearchKeyword = string.Empty;
        FieldMappings.Clear();
        CategoryMappings.Clear();
        TypeStatusMappings.Clear();
        PaymentMethodMappings.Clear();
        PaymentMethodGroups.Clear();
        ImportStatus = "请拖拽或选择文件导入";
    }

    private static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }
}
