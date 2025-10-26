using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [JsonPropertyName("frequency")]
    public int Frequency { get; set; }

    [JsonPropertyName("register_units")]
    public List<GeminiRegisterUnit> RegisterUnits { get; set; }
}
