using Cortex.Services.Interfaces;
using System.Text.Json;

namespace Cortex.Services;

public class GeminiResponseHandler(ILogger<GeminiResponseHandler> logger) : IGeminiResponseHandler
{
    private readonly ILogger<GeminiResponseHandler> _logger = logger;

    /// <summary>
    /// Processa a resposta JSON do Gemini, realizando sanitização e desserialização.
    /// </summary>
    /// <param name="jsonResponse">Resposta JSON bruta do Gemini</param>
    /// <returns>Objeto GeminiIndexResponse desserializado e validado</returns>
    /// <exception cref="InvalidOperationException">Quando a resposta está vazia</exception>
    /// <exception cref="JsonException">Quando a desserialização falha ou estrutura é inválida</exception>
    public T ParseResponse<T>(string jsonResponse) where T : class // Adiciona restrição para tipos de referência
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogError("Resposta JSON vazia ou nula recebida.");
            throw new ArgumentNullException(nameof(jsonResponse), "A resposta JSON não pode ser vazia ou nula.");
        }

        string sanitized = Util.Util.SanitizeGeminiJsonResponse(jsonResponse);
        _logger.LogDebug("JSON Sanitizado: {SanitizedJson}", sanitized);

        try
        {
            JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

            T? response = JsonSerializer.Deserialize<T>(sanitized, options);

            if (response == null)
            {
                _logger.LogError("Desserialização resultou em um objeto nulo para o tipo {TypeName}.", typeof(T).Name);
                throw new JsonException($"Falha na desserialização: o resultado para {typeof(T).Name} é nulo.");
            }

            _logger.LogInformation("Resposta JSON desserializada com sucesso para o tipo {TypeName}.", typeof(T).Name);
            return response;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Erro de desserialização JSON para o tipo {TypeName}. JSON Sanitizado: {SanitizedJson}", typeof(T).Name, sanitized);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante o processamento da resposta JSON para o tipo {TypeName}.", typeof(T).Name);
            throw new JsonException($"Erro inesperado ao processar JSON para {typeof(T).Name}: {ex.Message}", ex);
        }
    }
}
