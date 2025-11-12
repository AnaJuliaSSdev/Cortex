namespace Cortex.Models.DTO;

public class ExportRequest
{
    public int AnalysisId { get; set; }
    public byte[]? ChartImage { get; set; }
    public ExportOptions Options { get; set; } = new();
}
