using Cortex.Models;
using Cortex.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Cortex.Exceptions;

namespace Cortex.Services;

public class EmbeddingService(IOptions<GeminiConfiguration> settings, ILogger<EmbeddingService> logger, HttpClient httpClient) : IEmbeddingService
{
    private readonly GeminiConfiguration _settings = settings.Value;
    private readonly ILogger<EmbeddingService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private const string URL_GEMINI_EMBEDDING = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:batchEmbedContents?key=";
    private const string GEMINI_EMBEDDING_MODEL = "models/text-embedding-004";

    public float CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException("Embeddings must have the same dimension");

        float dotProduct = 0f;
        float norm1 = 0f;
        float norm2 = 0f;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        return dotProduct / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
        {
            return [];
        }

        StringBuilder url = new(URL_GEMINI_EMBEDDING);
        url.Append($"{_settings.ApiKey}");

        var requestBody = new
        {
            requests = texts.Select(text => new
            {
                model = GEMINI_EMBEDDING_MODEL,
                content = new
                {
                    parts = new[] { new { text } }
                }
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url.ToString(), content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(message: $"Gemini API Error (batch): {response.StatusCode} - {errorContent}");
            throw new GeminiAPIErrorException(response.StatusCode.ToString());
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        GeminiBatchEmbeddingResponse? batchEmbeddingResponse = JsonSerializer.Deserialize<GeminiBatchEmbeddingResponse>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return batchEmbeddingResponse?.Embeddings?
            .Select(e => e.Values)
            .ToList() ?? [];
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var result = await GenerateEmbeddingsAsync([text]);
        return result.FirstOrDefault() ?? throw new FailedToGenerateEmbeddingsException();
    }

    public async Task<List<float[]>> SelectMostRelevantEmbeddingsToQuestion(List<float[]> embeddings, string question)
    {
        var questionEmbedding = await GenerateEmbeddingAsync(question);

        var similarities = embeddings
           .Select(e => new
           {
               Embedding = e,
               Similarity = CalculateSimilarity(questionEmbedding, e)
           })
           .OrderByDescending(x => x.Similarity)
           .Take(5)
           .Select(x => x.Embedding)
           .ToList();

        return similarities;
    }

    
}
