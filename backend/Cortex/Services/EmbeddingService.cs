using Cortex.Models;
using Cortex.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

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

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_settings.ApiKey}";

            var requestBody = new
            {
                content = new
                {
                    parts = new[]
                    {
                        new { text }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Erro na API do Gemini: {response.StatusCode} - {errorContent}");
                throw new Exception($"Erro na API do Gemini: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return embeddingResponse?.Embedding?.Values ?? throw new Exception("Resposta inválida da API de embeddings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar embedding para o texto");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var embeddings = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text);
            embeddings.Add(embedding);
        }

        return embeddings;
    }
}

public class GeminiEmbeddingResponse
{
    public EmbeddingData? Embedding { get; set; }
}

public class EmbeddingData
{
    public float[] Values { get; set; } = Array.Empty<float>();
}