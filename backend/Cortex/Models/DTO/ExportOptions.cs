namespace Cortex.Models.DTO;

public class ExportOptions
{
    public bool IncludeChartImage { get; set; } = true;
    public bool IncludeSummaryTable { get; set; } = true;
    public bool IncludeDetailedData { get; set; } = true;
    public bool IncludeReferences { get; set; } = true;
    public string? CustomTitle { get; set; }
}
