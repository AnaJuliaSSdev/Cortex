namespace Cortex.Models;

public class GeminiConfiguration
{
    public const string SectionName = "GeminiConfiguration";

    public required string ApiKey { get; set; }
    public required string DefaultModel { get; set; }
}
