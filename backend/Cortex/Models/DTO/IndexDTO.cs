using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class IndexDTO
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("indicator")]
    public string IndicatorName { get; set; } // Renomeado para clareza
}
