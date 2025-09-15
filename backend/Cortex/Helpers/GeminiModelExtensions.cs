using Cortex.Models.Enums;

namespace GeminiService.Api.Extensions;

public static class GeminiModelExtensions
{
    public static string ToApiString(this GeminiModel model)
    {
        return model switch
        {
            GeminiModel.GeminiPro => "gemini-1.0-pro",
            GeminiModel.GeminiProVision => "gemini-pro-vision",
            GeminiModel.Gemini15Pro => "gemini-1.5-pro",
            GeminiModel.Gemini15Flash => "gemini-1.5-flash",
            _ => throw new NotSupportedException($"The Gemini model '{model}' it is not supported.")
        };
    }
}