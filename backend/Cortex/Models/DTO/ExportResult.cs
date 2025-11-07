namespace Cortex.Models.DTO;

public class ExportResult
{
    public bool Success { get; set; }
    public byte[]? FileContent { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? ErrorMessage { get; set; }
}
