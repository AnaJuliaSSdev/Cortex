using Cortex.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public record GeminiRequest(
    [Required] string Prompt,
    GeminiModel? Model,
    string? ApiKey
);
