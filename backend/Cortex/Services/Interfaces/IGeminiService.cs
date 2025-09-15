using Cortex.Models.Enums;
using Cortex.Models;

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
}
