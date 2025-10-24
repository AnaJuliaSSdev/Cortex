﻿using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using GeminiService.Api.Extensions;
using GenerativeAI;
using Google.Cloud.AIPlatform.V1;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace GeminiService.Api.Services.Implementations;

public class GeminiService(ILogger<GeminiService> logger, IOptions<GeminiConfiguration> settings) : IGeminiService
{
    private readonly GeminiConfiguration _settings = settings.Value;
    private readonly ILogger<GeminiService> _logger = logger;
    private static readonly HttpClient _httpClient = new();

    //CONSTANTES DE CONFIGURAÇÃO VERTEX
    private const string ProjectId = "cortex-472122";
    private const string GcsBucketName = "cortex-analysis";
    private const string Location = "us-central1";
    private const string ModelId = "gemini-2.5-pro";


    //ONLY TEXT
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

            return new GeminiResponse { IsSuccess = true, FullResponse = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao chamar a API do Gemini.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = $"Erro ao se comunicar com a API: {ex.Message}" };
        }
    }

    public async Task<string> GenerateContentWithDocuments(List<DocumentInfo> documents, string prompt, float temperature = 0.4f, int maxOutputTokens = 8192)
    {
        var predictionServiceClient = new PredictionServiceClientBuilder().Build();

        var allParts = new List<Part>();

        allParts.Add(new Part { Text = prompt });
        foreach (var doc in documents)
        {
            allParts.Add(new Part
            {
                FileData = new FileData
                {
                    FileUri = doc.GcsUri,
                    MimeType = doc.MimeType
                }
            });
        }

        var generationConfig = new GenerationConfig
        {
            //MaxOutputTokens = maxOutputTokens,
            Temperature = temperature,
            TopP = 1.0f
        };

        var generateContentRequest = new GenerateContentRequest
        {
            Model = $"projects/{ProjectId}/locations/{Location}/publishers/google/models/{ModelId}",
            Contents =
            {
                new Content
                {
                    Role = "USER",
                }
            },
            GenerationConfig = generationConfig
        };

        generateContentRequest.Contents[0].Parts.AddRange(allParts);
        GenerateContentResponse response = await predictionServiceClient.GenerateContentAsync(generateContentRequest);

        if (response.Candidates.Any() && response.Candidates.First().Content.Parts.Any())
        {
            return response.Candidates.First().Content.Parts.First().Text;
        }

        return String.Empty; // significa que o modelo não retornou nada
    }
}