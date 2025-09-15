using Microsoft.Extensions.Options;
using Cortex.Models;
using Cortex.Services.Interfaces;
using GenerativeAI;
using Cortex.Models.Enums;
using GeminiService.Api.Extensions;

namespace GeminiService.Api.Services.Implementations;

public class GeminiService(IOptions<GeminiConfiguration> settings, ILogger<GeminiService> logger) : IGeminiService
{
    private readonly GeminiConfiguration _settings = settings.Value;
    private readonly ILogger<GeminiService> _logger = logger;

    public async Task<GeminiResponse> GenerateTextAsync(string prompt, Cortex.Models.Enums.GeminiModel? modelOverride = null, string? apiKeyOverride = null)
    {
        var apiKey = apiKeyOverride ?? _settings.ApiKey;
        var modelName = modelOverride.HasValue
            ? modelOverride.Value.ToApiString()
            : _settings.DefaultModel;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("API Key do Gemini não foi configurada.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = "API Key do Gemini não está configurada." };
        }

        try
        {
            var model = new GenerativeModel(apiKey, model: modelName);

            var response = await model.GenerateContentAsync(prompt);

            return new GeminiResponse { IsSuccess = true, Content = response.Text() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao chamar a API do Gemini.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = $"Erro ao se comunicar com a API: {ex.Message}" };
        }
    }
}