using Cortex.Models;
using Cortex.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Cortex.Exceptions;

namespace Cortex.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly GeminiConfiguration _settings;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly HttpClient _httpClient;


    public EmbeddingService(IOptions<GeminiConfiguration> settings, ILogger<EmbeddingService> logger, HttpClient httpClient)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }


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
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:batchEmbedContents?key={_settings.ApiKey}";

        var requestBody = new
        {
            requests = texts.Select(text => new
            {
                model = "models/text-embedding-004",
                content = new
                {
                    parts = new[] { new { text } }
                }
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(message: $"Gemini API Error (batch): {response.StatusCode} - {errorContent}");
            throw new GeminiAPIErrorException(response.StatusCode.ToString());
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var batchEmbeddingResponse = JsonSerializer.Deserialize<GeminiBatchEmbeddingResponse>(jsonResponse, new JsonSerializerOptions
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
}

public class GeminiBatchEmbeddingResponse
{
    public List<EmbeddingData>? Embeddings { get; set; }
}

public class EmbeddingData
{
    public float[] Values { get; set; } = Array.Empty<float>();
}

public class GeminiEmbeddingResponse
{
    public EmbeddingData? Embedding { get; set; }
}
