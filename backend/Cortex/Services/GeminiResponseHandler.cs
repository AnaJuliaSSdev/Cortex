using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using System.Text.Json;

namespace Cortex.Services;

public class GeminiResponseHandler : IGeminiResponseHandler
{
    private readonly ILogger<GeminiResponseHandler> _logger;

    public GeminiResponseHandler(ILogger<GeminiResponseHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processa a resposta JSON do Gemini, realizando sanitização e desserialização.
    /// </summary>
    /// <param name="jsonResponse">Resposta JSON bruta do Gemini</param>
    /// <returns>Objeto GeminiIndexResponse desserializado e validado</returns>
    /// <exception cref="InvalidOperationException">Quando a resposta está vazia</exception>
    /// <exception cref="JsonException">Quando a desserialização falha ou estrutura é inválida</exception>
    public GeminiIndexResponse ParseResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogError("Resposta vazia recebida do Gemini.");
            throw new InvalidOperationException("O serviço de IA retornou uma resposta vazia.");
        }

        string sanitized = Util.Util.SanitizeGeminiJsonResponse(jsonResponse);

        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        GeminiIndexResponse response = JsonSerializer.Deserialize<GeminiIndexResponse>(sanitized, options);

        if (response == null || response.Indices == null)
        {
            _logger.LogError("Falha na desserialização: objeto ou propriedade 'Indices' está nulo.");
            throw new JsonException("Falha ao desserializar a resposta da IA. Indices não foram encontrados.");
        }

        return response;
    }
}
