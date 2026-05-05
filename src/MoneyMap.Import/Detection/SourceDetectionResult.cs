using MoneyMap.Core.Enums;

namespace MoneyMap.Import.Detection;

public class SourceDetectionResult
{
    public DataSource Source { get; set; }
    public double Confidence { get; set; }
    public string? DetectedBy { get; set; }
    public bool IsConfident => Confidence >= 0.8;
}
