using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiIndex
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("indicator")]
    public string Indicator { get; set; } // O JSON retorna o *nome* do indicador

    [JsonPropertyName("references")]
    public List<GeminiReference> References { get; set; }
}
