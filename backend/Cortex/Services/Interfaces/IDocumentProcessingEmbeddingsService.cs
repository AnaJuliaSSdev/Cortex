using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IDocumentProcessingEmbeddingsService
{
    Task ProcessAsync(Document document, int analysisId);
}
