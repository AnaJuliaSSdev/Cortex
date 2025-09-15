using Cortex.Models.Enums;

namespace Cortex.Models;

public static class GeminiModelExtensions
{
    private static readonly Dictionary<GeminiModel, string> ModelNames = new()
    {
        { GeminiModel.GeminiPro, "gemini-pro" },
        { GeminiModel.GeminiProVision, "gemini-pro-vision" },
        { GeminiModel.Gemini15Pro, "gemini-1.5-pro" },
        { GeminiModel.Gemini15Flash, "gemini-1.5-flash" }
    };

    public static string ToModelName(this GeminiModel model)
    {
        return ModelNames[model];
    }
}
