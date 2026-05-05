namespace MoneyMap.Utils;

/// <summary>
/// 文件大小格式化工具
/// </summary>
public static class FileSizeFormatter
{
    /// <summary>
    /// 将字节数格式化为可读的文件大小字符串
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化后的字符串，如 "1.5 KB"、"2.3 MB"</returns>
    public static string FormatFileSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
            _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
        };
    }
}
