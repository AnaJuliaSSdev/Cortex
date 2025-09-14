using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IDocumentProcessingStrategy
{
    Task<Document> ProcessAsync(IFormFile file);
}
