using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiCategoryResponse
{
    [JsonPropertyName("categories")]
    public List<GeminiCategory> Categories { get; set; }
}
