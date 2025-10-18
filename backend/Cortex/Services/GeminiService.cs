using Cortex.Models;
using Cortex.Services.Interfaces;
using GeminiService.Api.Extensions;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Options;
using System.Net.Mime;

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

    //MÉTODO QUE ENVIA ARQUIVOS, NÃO TESTADO AINDA E NÃO FUNCIONAL
    public async Task<GeminiResponse> GenerateContentAsync(string prompt, IEnumerable<Part> fileParts, Cortex.Models.Enums.GeminiModel? modelOverride = null, string? apiKeyOverride = null)
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
            // Configuração do modelo e das requisições
            var generationConfig = new GenerationConfig
            {
                // Importante: Configurar para receber JSON
                ResponseMimeType = MediaTypeNames.Application.Json,
            };

            var model = new GenerativeModel(apiKey, model: modelName, generationConfig: generationConfig);

            // Construir a lista de conteúdo para a requisição
            // O primeiro item é o prompt de texto, seguido pelos arquivos.
            var content = new List<Part> { new Part { Text = prompt } };
            content.AddRange(fileParts);

            var request = new GenerateContentRequest { Contents = [new Content { Parts = content }] };

            // Gerar o conteúdo
            var response = await model.GenerateContentAsync(request);

            // Retornar a resposta (sem tratamento, conforme solicitado)
            return new GeminiResponse { IsSuccess = true, RawResponse = response }; // Supondo que GeminiResponse possa carregar a resposta bruta
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao chamar a API do Gemini com múltiplos conteúdos.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = $"Erro ao se comunicar com a API: {ex.Message}" };
        }
    }
}