namespace Cortex.Models;

public class GeminiResponse
{
    public bool IsSuccess { get; set; }
    public string? Content { get; set; }
    public string? ErrorMessage { get; set; }
}
