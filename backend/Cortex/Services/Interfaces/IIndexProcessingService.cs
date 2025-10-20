using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IIndexProcessingService
{
    Task<List<Cortex.Models.Index>> ProcessGeminiResponseAsync(GeminiResponse geminiResponse, int analysisId);
}
