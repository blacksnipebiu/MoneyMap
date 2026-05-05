using System.Globalization;
using Bookkeeping.Core.Enums;
using Bookkeeping.Core.Models;
using MiniExcelLibs;
using Bookkeeping.Import.Mapping;

namespace Bookkeeping.Import.Parsers;

public class WeChatPayParser : IRecordParser
{
    public DataSource SupportedSource => DataSource.WeChatPay;

    public ParseResult Parse(Stream fileStream, string fileName)
    {
        var result = new ParseResult
        {
            DetectedSource = DataSource.WeChatPay
        };

        try
        {
            fileStream.Position = 0;
            var rows = fileStream.Query(useHeaderRow: false).ToList();

            // Find header row (contains "交易时间")
            int headerRowIndex = -1;
            for (int i = 0; i < Math.Min(20, rows.Count); i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;

                var values = row.Values.ToList();
                if (values.Count > 0 && values[0]?.ToString() == WeChatFieldMapping.TransactionTime)
                {
                    headerRowIndex = i;
                    break;
                }
            }

            if (headerRowIndex < 0)
            {
                result.Errors.Add(new ParseError { ErrorMessage = "Cannot find header row in WeChat Excel" });
                return result;
            }

            // Get header names
            var headerRow = rows[headerRowIndex] as IDictionary<string, object>;
            if (headerRow == null)
            {
                result.Errors.Add(new ParseError { ErrorMessage = "Invalid header row" });
                return result;
            }

            var columnNames = headerRow.Values.Select(v => v?.ToString() ?? "").ToList();

            // Parse data rows
            result.TotalRows = rows.Count - headerRowIndex - 1;

            for (int i = headerRowIndex + 1; i < rows.Count; i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;

                var values = row.Values.ToList();
                if (values.Count < 8 || values.All(v => string.IsNullOrWhiteSpace(v?.ToString())))
                {
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    var GetValue = (int index) => index < values.Count ? values[index]?.ToString()?.Trim() : null;

                    var timeStr = GetValue(0);
                    if (string.IsNullOrEmpty(timeStr))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    if (!TryParseDateTime(timeStr, out var transactionTime))
                    {
                        result.Errors.Add(new ParseError
                        {
                            RowNumber = i + 1,
                            ErrorMessage = $"Cannot parse date: {timeStr}",
                            FieldName = "TransactionTime"
                        });
                        continue;
                    }

                    var direction = GetValue(4) ?? "/";
                    var rawStatus = GetValue(7) ?? "";
                    var status = StatusMapping.MapStatus(rawStatus, DataSource.WeChatPay);
                    
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
                            "收入" => TransactionType.Income,
                            "/" => DetectWeChatTransferType(GetValue(1), GetValue(2), GetValue(3), GetValue(6)),
                            _ => DetectWeChatTransferType(GetValue(1), GetValue(2), GetValue(3), GetValue(6))
                        };

                        // 智能识别：即使方向是支出/收入，也可能是转账
                        if (transactionType != TransactionType.Transfer)
                        {
                            transactionType = MaybeWeChatTransfer(transactionType, GetValue(1), GetValue(3), GetValue(2));
                        }
                    }

                    // Parse amount (remove ¥ prefix)
                    var amountStr = GetValue(5) ?? "0";
                    amountStr = amountStr.Replace("¥", "").Replace("￥", "").Trim();
                    if (!decimal.TryParse(amountStr, out var amount))
                    {
                        result.Errors.Add(new ParseError
                        {
                            RowNumber = i + 1,
                            ErrorMessage = $"Cannot parse amount: {amountStr}"
                        });
                        continue;
                    }

                    var description = GetValue(3) ?? "";
                    var remark = GetValue(10) ?? "";
                    var merchantOrderId = GetValue(9) ?? "";
                    
                    // Remark 和 Description 保持独立
                    var cleanRemark = remark;
                    if (string.IsNullOrEmpty(cleanRemark) || cleanRemark == "/")
                        cleanRemark = null;

                    var transaction = new Transaction
                    {
                        TransactionTime = transactionTime,
                        Type = transactionType,
                        Amount = amount,
                        Counterparty = GetValue(2),
                        Description = description,
                        CategoryName = GetValue(1),
                        PaymentMethod = GetValue(6),
                        Status = status,
                        RawStatus = rawStatus,
                        Remark = cleanRemark,
                        MerchantOrderId = merchantOrderId,
                        Source = DataSource.WeChatPay,
                        DataSourceName = "微信支付",
                        SourceTransactionId = GetValue(8),
                        RawLine = System.Text.Json.JsonSerializer.Serialize(values),
                        CreatedAt = DateTime.Now
                    };

                    result.Transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = i + 1,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ParseError { ErrorMessage = $"Parse error: {ex.Message}" });
        }

        return result;
    }

    /// <summary>
    /// 微信 "/" 方向的交易，判断是否为转账
    /// </summary>
    private static TransactionType DetectWeChatTransferType(string? transactionType, string? counterparty, string? product, string? paymentMethod)
    {
        var text = $"{transactionType} {counterparty} {product} {paymentMethod}";
        var transferKeywords = new[] { "转账", "提现", "充值", "零钱通", "转入", "转出", "红包", "群收款" };
        foreach (var keyword in transferKeywords)
        {
            if (text.Contains(keyword))
                return TransactionType.Transfer;
        }
        return TransactionType.Transfer;
    }

    /// <summary>
    /// 即使方向是支出/收入，也检查是否可能是转账场景
    /// （如：微信零钱转到银行卡、信用卡还款等）
    /// </summary>
    private static TransactionType MaybeWeChatTransfer(TransactionType currentType, string? transactionType, string? product, string? counterparty)
    {
        var text = $"{transactionType} {product} {counterparty}";
        if (text.Contains("转账-") || text.Contains("转账到") || text.Contains("提现到") ||
            text.Contains("信用卡还款") || text.Contains("还信用卡") || text.Contains("微粒贷还款"))
        {
            return TransactionType.Transfer;
        }
        return currentType;
    }

    public bool CanParse(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
            return false;

        try
        {
            fileStream.Position = 0;
            var rows = fileStream.Query(useHeaderRow: false, startCell: "A1").Take(20).ToList();

            foreach (var row in rows)
            {
                var dict = row as IDictionary<string, object>;
                if (dict == null) continue;

                var values = dict.Values.ToList();
                if (values.Count > 0 && values[0]?.ToString() == WeChatFieldMapping.TransactionTime)
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
