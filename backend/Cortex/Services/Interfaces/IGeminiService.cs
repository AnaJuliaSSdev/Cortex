using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using GenerativeAI.Types;
using System.Net.Http;
using System.Text.Json;
using static GeminiService.Api.Services.Implementations.GeminiService;

namespace Cortex.Services.Interfaces;

public interface IGeminiService
{
    /// <summary>
    /// Gera conteúdo de texto com base em um prompt.
    /// Permite a substituição opcional do modelo e da chave de API configurados por padrão.
    /// </summary>
    /// <param name="prompt">O texto de entrada para o modelo.</param>
    /// <param name="modelOverride">Opcional. Substitui o modelo padrão configurado.</param>
    /// <param name="apiKeyOverride">Opcional. Substitui a chave de API padrão configurada.</param>
    /// <returns>Um objeto GeminiResponse com o conteúdo gerado ou uma mensagem de erro.</returns>
    Task<GeminiResponse> GenerateTextAsync(
        string prompt,
        GeminiModel? modelOverride = null,
        string? apiKeyOverride = null);

    Task<string> GenerateContentWithDocuments(List<DocumentInfo> documents, string prompt, float temperature = 0.4f, int maxOutputTokens = 8192);
}
