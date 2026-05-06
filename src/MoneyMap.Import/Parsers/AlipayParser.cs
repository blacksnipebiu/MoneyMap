using System.Globalization;
using System.Text;
using MoneyMap.Core.Enums;
using MoneyMap.Core.Models;
using MoneyMap.Import.Mapping;
using CsvHelper;
using CsvHelper.Configuration;

namespace MoneyMap.Import.Parsers;

public class AlipayParser : IRecordParser
{
    public DataSource SupportedSource => DataSource.Alipay;

    public ParseResult Parse(Stream fileStream, string fileName)
    {
        var result = new ParseResult
        {
            DetectedSource = DataSource.Alipay
        };

        try
        {
            // Register code page encoding provider for GBK support
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Try UTF-8 first, then GBK
            var (lines, encoding) = ReadAllLines(fileStream);

            // Find header row (starts with "交易时间")
            int headerRowIndex = -1;
            for (int i = 0; i < Math.Min(30, lines.Count); i++)
            {
                if (lines[i].StartsWith(AlipayFieldMapping.TransactionTime))
                {
                    headerRowIndex = i;
                    break;
                }
            }

            if (headerRowIndex < 0)
            {
                result.Errors.Add(new ParseError { ErrorMessage = "Cannot find header row in Alipay CSV" });
                return result;
            }

            // Parse with CsvHelper
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
                Delimiter = ","
            };

            var headerLine = lines[headerRowIndex];
            var dataLines = lines.Skip(headerRowIndex + 1).ToList();
            result.TotalRows = dataLines.Count;

            using var textReader = new StringReader(headerLine + "\n" + string.Join("\n", dataLines));
            using var csv = new CsvReader(textReader, config);

            csv.Read();
            csv.ReadHeader();

            int rowNum = headerRowIndex + 2; // 1-based, accounting for header
            while (csv.Read())
            {
                try
                {
                    var rawLine = csv.Parser.RawRecord ?? "";

                    // Skip empty lines or separator lines
                    if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("---"))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    var rawStatus = csv.GetField<string>(AlipayFieldMapping.Status)?.Trim() ?? "";
                    var status = StatusMapping.MapStatus(rawStatus, DataSource.Alipay);

                    var direction = csv.GetField<string>(AlipayFieldMapping.Direction)?.Trim() ?? "";
                    var description = csv.GetField<string>(AlipayFieldMapping.Description)?.Trim() ?? "";
                    var remark = csv.GetField<string>(AlipayFieldMapping.Remark)?.Trim() ?? "";
                    var counterparty = csv.GetField<string>(AlipayFieldMapping.Counterparty)?.Trim();
                    var paymentMethod = csv.GetField<string>(AlipayFieldMapping.PaymentMethod)?.Trim();

                    // 退款状态：无论方向如何，退款归类为 Income
                    TransactionType transactionType;
                    if (status == TransactionStatus.Refunded)
                    {
                        transactionType = TransactionType.Income;
                    }
                    else
                    {
                        transactionType = direction switch
                        {
                            "支出" => TransactionType.Expense,
                            "收入" => DetectIncomeOrCurrent(description, counterparty, paymentMethod),
                            "不计收支" => DetectTransferOrIncome(description, counterparty, paymentMethod),
                            _ => DetectTransferOrIncome(description, counterparty, paymentMethod)
                        };

                        // 智能识别：即使方向是支出/收入，也可能是转账
                        if (transactionType != TransactionType.Transfer)
                        {
                            transactionType = MaybeTransfer(transactionType, description, counterparty, paymentMethod);
                        }
                    }

                    var amountStr = csv.GetField<string>(AlipayFieldMapping.Amount)?.Trim() ?? "0";
                    if (!decimal.TryParse(amountStr, out var amount))
                    {
                        result.Errors.Add(new ParseError
                        {
                            RowNumber = rowNum,
                            RawLine = rawLine,
                            ErrorMessage = $"无法解析金额: {amountStr}",
                            FieldName = "Amount",
                            RawValue = amountStr
                        });
                        continue;
                    }

                    var timeStr = csv.GetField<string>(AlipayFieldMapping.TransactionTime)?.Trim() ?? "";
                    if (!TryParseDateTime(timeStr, out var transactionTime))
                    {
                        result.Errors.Add(new ParseError
                        {
                            RowNumber = rowNum,
                            RawLine = rawLine,
                            ErrorMessage = $"Cannot parse date: {timeStr}",
                            FieldName = "TransactionTime"
                        });
                        continue;
                    }

                    var sourceTransactionId = csv.GetField<string>(AlipayFieldMapping.TransactionId)?.Trim();
                    var merchantOrderId = csv.GetField<string>(AlipayFieldMapping.MerchantOrderId)?.Trim();
                    var counterpartyAccount = csv.GetField<string>(AlipayFieldMapping.CounterpartyAccount)?.Trim();

                    // Remark 和 Description 保持独立，不再合并
                    var cleanRemark = remark;
                    if (string.IsNullOrEmpty(cleanRemark) || cleanRemark == "/")
                        cleanRemark = null;

                    var transaction = new Transaction
                    {
                        TransactionTime = transactionTime,
                        Type = transactionType,
                        Amount = amount,
                        Counterparty = counterparty,
                        CounterpartyAccount = counterpartyAccount,
                        Description = description,
                        CategoryName = csv.GetField<string>(AlipayFieldMapping.Category)?.Trim(),
                        PaymentMethod = paymentMethod,
                        Status = status,
                        RawStatus = rawStatus,
                        Remark = cleanRemark,
                        MerchantOrderId = merchantOrderId,
                        Source = DataSource.Alipay,
                        DataSourceName = "支付宝",
                        SourceTransactionId = sourceTransactionId,
                        RawLine = rawLine,
                        CreatedAt = DateTime.Now
                    };

                    result.Transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = rowNum,
                        ErrorMessage = $"解析行失败: {ex.Message}",
                        FieldName = "Row"
                    });
                }
                finally
                {
                    rowNum++;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ParseError { ErrorMessage = $"Parse error: {ex.Message}" });
        }

        return result;
    }

    public bool CanParse(Stream fileStream, string fileName)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var (lines, _) = ReadAllLines(fileStream);

            for (int i = 0; i < Math.Min(30, lines.Count); i++)
            {
                if (lines[i].Contains(AlipayFieldMapping.TransactionTime) &&
                    lines[i].Contains(AlipayFieldMapping.TransactionId))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore
        }

        return false;
    }

/// <summary>
    /// "不计收支"方向的交易，优先识别理财收益→Income，其余→Transfer
    /// </summary>
    private static TransactionType DetectTransferOrIncome(string description, string? counterparty, string? paymentMethod)
    {
        var text = $"{description} {counterparty} {paymentMethod}";

        // 优先检测理财收益 → Income（如："余额宝-2026.05.03-收益发放"）
        if (IsInvestmentIncome(description, text))
            return TransactionType.Income;

        // 转账/提现/充值/理财买入 等关键词 → Transfer
        var transferKeywords = new[] { "转账", "提现", "充值", "转入", "转出", "提款" };
        foreach (var keyword in transferKeywords)
        {
            if (text.Contains(keyword))
                return TransactionType.Transfer;
        }

        // 余额宝/余利宝/蚂蚁财富等理财操作（非收益）→ Transfer
        var investmentKeywords = new[] { "余额宝", "余利宝", "蚂蚁财富", "基金", "理财产品" };
        foreach (var keyword in investmentKeywords)
        {
            if (text.Contains(keyword))
                return TransactionType.Transfer;
        }

        // 默认不计收支归为 Transfer
        return TransactionType.Transfer;
    }

    /// <summary>
    /// "收入"方向的交易，检测是否实际为理财收益（保持 Income）或应转为 Transfer
    /// </summary>
    private static TransactionType DetectIncomeOrCurrent(string description, string? counterparty, string? paymentMethod)
    {
        var text = $"{description} {counterparty} {paymentMethod}";

        // 理财收益 → Income（如："余额宝-收益发放"、"基金分红"）
        if (IsInvestmentIncome(description, text))
            return TransactionType.Income;

        // 余额宝/余利宝等其他操作（转入等）→ Transfer
        if (text.Contains("余额宝") || text.Contains("余利宝"))
            return TransactionType.Transfer;

        return TransactionType.Income;
    }

    /// <summary>
    /// 判断描述是否为理财收益（收益、利息、分红等）
    /// </summary>
    private static bool IsInvestmentIncome(string description, string text)
    {
        var incomeKeywords = new[] { "收益发放", "收益到账", "利息", "分红", "派息", "赎回收益" };
        foreach (var keyword in incomeKeywords)
        {
            if (description.Contains(keyword))
                return true;
        }

        // "余额宝-.*-收益" 或 "零钱通-.*-收益" 模式
        if ((description.Contains("余额宝") || description.Contains("余利宝")) && description.Contains("收益"))
            return true;

        return false;
    }

    /// <summary>
    /// 即使方向是支出/收入，也检查是否可能是转账场景
    /// （如：支付宝余额转到银行卡、信用卡还款、理财买入等）
    /// </summary>
    private static TransactionType MaybeTransfer(TransactionType currentType, string description, string? counterparty, string? paymentMethod)
    {
        var text = $"{description} {counterparty} {paymentMethod}";

        // 明确的转账场景
        if (text.Contains("转账-") || text.Contains("转账到") || text.Contains("提现到") ||
            text.Contains("信用卡还款") || text.Contains("还信用卡") || text.Contains("花呗还款"))
        {
            return TransactionType.Transfer;
        }

        // 理财买入/卖出 → Transfer（如："蚂蚁财富-招商中证白酒指数C-买入"）
        var investBuySellKeywords = new[] { "买入", "卖出", "申购", "赎回" };
        var investPlatformKeywords = new[] { "蚂蚁财富", "余额宝", "余利宝", "基金", "理财" };
        var hasInvestPlatform = investPlatformKeywords.Any(k => text.Contains(k));
        var hasBuySellAction = investBuySellKeywords.Any(k => description.Contains(k));
        if (hasInvestPlatform && hasBuySellAction)
            return TransactionType.Transfer;

return currentType;
    }

    private (List<string> lines, Encoding encoding) ReadAllLines(Stream stream)
    {
        // Try UTF-8 first
        stream.Position = 0;
        using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
        {
            var content = reader.ReadToEnd();
            // Check for valid UTF-8: no replacement characters and reasonable Chinese character ratio
            if (!content.Contains('\0') && !content.Contains('\uFFFD'))
            {
                // Additional check: if content has Chinese characters, verify they're valid
                var chineseCount = content.Count(c => c >= 0x4E00 && c <= 0x9FFF);
                if (chineseCount > 0 || content.Length < 100)
                {
                    return (content.Split('\n').ToList(), Encoding.UTF8);
                }
            }
        }

        // Fall back to GBK (common for older Alipay exports)
        stream.Position = 0;
        using (var reader = new StreamReader(stream, Encoding.GetEncoding("GBK"), leaveOpen: true))
        {
            var content = reader.ReadToEnd();
            return (content.Split('\n').ToList(), Encoding.GetEncoding("GBK"));
        }
    }

    /// <summary>
    /// 尝试解析日期时间，支持多种格式
    /// </summary>
    private static bool TryParseDateTime(string input, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 支持的日期格式
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy/MM/dd HH:mm",
            "yyyy-MM-dd",
            "yyyy/MM/dd"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;
        }

        // 最后尝试自动解析
        return DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}