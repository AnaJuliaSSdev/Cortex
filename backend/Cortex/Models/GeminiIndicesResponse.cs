using Cortex.Models.DTO;
using System.Text.Json.Serialization;

namespace Cortex.Models;

public class GeminiIndicesResponse
{
    [JsonPropertyName("indices")]
    public List<IndexDTO> Indices { get; set; }
}
