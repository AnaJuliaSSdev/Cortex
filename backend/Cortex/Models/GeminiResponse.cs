using GenerativeAI.Types;
using System.Text.Json.Serialization;

namespace Cortex.Models;

public class GeminiResponse
{
    public bool IsSuccess { get; set; }
    public GenerateContentResponse FullResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Content => FullResponse?.Text;
    public Candidate[]? Candidates { get; set; }
}
