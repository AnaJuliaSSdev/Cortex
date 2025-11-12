using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiReference
{
    [JsonPropertyName("document")]
    public string Document { get; set; } // Nome do arquivo

    [JsonPropertyName("page")]
    public string Page { get; set; } // JSON retorna como string

    [JsonPropertyName("quoted_content")]
    public string QuotedContent { get; set; } // JSON retorna como string
}
