namespace MoneyMap.Utils;

/// <summary>
/// 支付方式名称处理工具
/// </summary>
public static class PaymentMethodHelper
{
    /// <summary>
    /// 提取支付方式名称的根名（去除后缀变体）
    /// 规则：取 "&"、"（"、"("、"-" 之前的部分
    /// </summary>
    /// <param name="name">原始支付方式名称</param>
    /// <returns>提取的根名称</returns>
    /// <example>
    /// "余额宝&红包" → "余额宝"
    /// "招商银行(信用卡)" → "招商银行"
    /// "花呗-分期" → "花呗"
    /// </example>
    public static string ExtractRoot(string name)
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
}
