using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiIndexResponse
{
    [JsonPropertyName("indices")]
    public List<GeminiIndex> Indices { get; set; }
}
