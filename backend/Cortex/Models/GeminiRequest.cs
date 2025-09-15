using Cortex.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models;

public record GeminiRequest(
    [Required] string Prompt,
    GeminiModel? Model,
    string? ApiKey
);
