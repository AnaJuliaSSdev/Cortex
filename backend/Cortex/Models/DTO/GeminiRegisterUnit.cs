using System.Text.Json.Serialization;

namespace Cortex.Models.DTO;

public class GeminiRegisterUnit
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("document")]
    public string Document { get; set; }

    [JsonPropertyName("page")]
    public string Page { get; set; }
    
    //lista de ids dos indices
    [JsonPropertyName("found_indices")]
    public List<string> FoundIndices { get; set; }

    //Simboliza o ID do indicador
    [JsonPropertyName("indicator")]
    public string Indicator { get; set; } 

    [JsonPropertyName("justification")]
    public string Justification { get; set; }
}
