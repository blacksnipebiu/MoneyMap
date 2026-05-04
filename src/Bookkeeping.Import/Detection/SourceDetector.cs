using System.Text;
using Bookkeeping.Core.Enums;
using MiniExcelLibs;
using Bookkeeping.Import.Mapping;

namespace Bookkeeping.Import.Detection;

public class SourceDetector
{
    public SourceDetectionResult Detect(string fileName, Stream? fileStream = null)
    {
        var result = new SourceDetectionResult();
        
        // Check by filename first
        if (fileName.Contains("支付宝", StringComparison.OrdinalIgnoreCase))
        {
            result.Source = DataSource.Alipay;
            result.Confidence = 0.9;
            result.DetectedBy = "Filename contains '支付宝'";
            return result;
        }
        
        if (fileName.Contains("微信", StringComparison.OrdinalIgnoreCase))
        {
            result.Source = DataSource.WeChatPay;
            result.Confidence = 0.9;
            result.DetectedBy = "Filename contains '微信'";
            return result;
        }
        
        if (fileName.Contains("银行", StringComparison.OrdinalIgnoreCase))
        {
            result.Source = DataSource.BankCard;
            result.Confidence = 0.7;
            result.DetectedBy = "Filename contains '银行'";
            return result;
        }
        
        // Check by file content
        if (fileStream != null)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            if (extension == ".csv")
            {
                return DetectFromCsv(fileStream);
            }
            
            if (extension == ".xlsx" || extension == ".xls")
            {
                return DetectFromExcel(fileStream);
            }
        }
        
        result.Source = DataSource.Other;
        result.Confidence = 0;
        result.DetectedBy = "Unable to detect source";
        return result;
    }
    
    private SourceDetectionResult DetectFromCsv(Stream stream)
    {
        var result = new SourceDetectionResult();
        
        try
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            
            // Read first 30 lines to find header
            for (int i = 0; i < 30; i++)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                
                // Check for Alipay header
                if (line.Contains(AlipayFieldMapping.TransactionTime) && line.Contains(AlipayFieldMapping.TransactionId))
                {
                    result.Source = DataSource.Alipay;
                    result.Confidence = 0.95;
                    result.DetectedBy = "CSV header matches Alipay format";
                    return result;
                }
                
                // Check for bank statement keywords
                if (line.Contains("借方") || line.Contains("贷方") || line.Contains("余额"))
                {
                    result.Source = DataSource.BankCard;
                    result.Confidence = 0.8;
                    result.DetectedBy = "CSV header contains bank keywords";
                    return result;
                }
            }
        }
        catch
        {
            // Ignore encoding errors
        }
        
        result.Source = DataSource.Other;
        result.Confidence = 0;
        return result;
    }
    
    private SourceDetectionResult DetectFromExcel(Stream stream)
    {
        var result = new SourceDetectionResult();
        
        try
        {
            stream.Position = 0;
            var rows = stream.Query(useHeaderRow: false, startCell: "A1").ToList();
            
            // Check first 20 rows for WeChat header
            for (int i = 0; i < Math.Min(20, rows.Count); i++)
            {
                var row = rows[i] as IDictionary<string, object>;
                if (row == null) continue;
                
                var values = row.Values.ToList();
                if (values.Count == 0) continue;
                
                var firstValue = values[0]?.ToString() ?? "";
                
                // Check for WeChat header
                if (firstValue.Contains(WeChatFieldMapping.TransactionTime) || 
                    (values.Count > 8 && values[5]?.ToString()?.Contains("金额") == true))
                {
                    result.Source = DataSource.WeChatPay;
                    result.Confidence = 0.95;
                    result.DetectedBy = "Excel header matches WeChat format";
                    return result;
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        
        result.Source = DataSource.Other;
        result.Confidence = 0;
        return result;
    }
}