using System.Text.RegularExpressions;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;

namespace MoneyMap.Data.Services;

/// <summary>
/// 导入映射服务 - 处理分类和账户映射
/// </summary>
public interface IImportMappingService
{
    /// <summary>
    /// 自动匹配分类
    /// </summary>
    Category? MatchCategory(string sourceCategory, TransactionType type, IEnumerable<Category> availableCategories);
    
    /// <summary>
    /// 自动匹配账户
    /// </summary>
    Account? MatchAccount(string paymentMethod, DataSource source, IEnumerable<Account> availableAccounts);
    
    /// <summary>
    /// 提取支付方式根名称（用于分组）
    /// </summary>
    string ExtractPaymentMethodRoot(string name);
}

public class ImportMappingService : IImportMappingService
{
    public Category? MatchCategory(string sourceCategory, TransactionType type, IEnumerable<Category> availableCategories)
    {
        if (string.IsNullOrWhiteSpace(sourceCategory) || sourceCategory == "/")
            return null;
        
        var categories = availableCategories.ToList();
        
        // 1. 精确匹配名称
        var exactMatch = categories.FirstOrDefault(c =>
            c.Type == type && string.Equals(c.Name, sourceCategory, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;
        
        // 2. 匹配 SourceCategoryMapping
        var mappingMatch = categories.FirstOrDefault(c =>
            c.Type == type &&
            c.SourceCategoryMapping != null &&
            c.SourceCategoryMapping.Split(',', ';').Contains(sourceCategory, StringComparer.OrdinalIgnoreCase));
        if (mappingMatch != null)
            return mappingMatch;
        
        // 3. 正则匹配 AutoMatchPattern
        var patternMatch = categories.FirstOrDefault(c =>
            c.Type == type &&
            c.AutoMatchPattern != null &&
            Regex.IsMatch(sourceCategory, c.AutoMatchPattern, RegexOptions.IgnoreCase));
        if (patternMatch != null)
            return patternMatch;
        
        return null;
    }
    
    public Account? MatchAccount(string paymentMethod, DataSource source, IEnumerable<Account> availableAccounts)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return null;
        
        var accounts = availableAccounts.ToList();
        
        // 1. 按支付方式名称匹配
        var nameMatch = accounts.FirstOrDefault(a =>
            paymentMethod.Contains(a.Name, StringComparison.OrdinalIgnoreCase));
        if (nameMatch != null)
            return nameMatch;
        
        // 2. 按来源匹配
        var sourceMatch = accounts.FirstOrDefault(a =>
            a.Source == source &&
            (paymentMethod.Contains(GetSourceKeyword(a.Source), StringComparison.OrdinalIgnoreCase) ||
             a.Name.Contains(paymentMethod, StringComparison.OrdinalIgnoreCase)));
        if (sourceMatch != null)
            return sourceMatch;
        
        // 3. 返回同来源的第一个账户
        return accounts.FirstOrDefault(a => a.Source == source);
    }
    
    public string ExtractPaymentMethodRoot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        
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
        
        return name;
    }
    
    private static string GetSourceKeyword(DataSource source) => source switch
    {
        DataSource.Alipay => "支付宝",
        DataSource.WeChatPay => "微信",
        DataSource.BankCard => "银行",
        _ => ""
    };
}
