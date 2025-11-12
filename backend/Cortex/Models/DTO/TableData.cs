namespace Cortex.Models.DTO;

public class TableData
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public string? Caption { get; set; }
}
