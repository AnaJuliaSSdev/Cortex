using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IDocumentProcessingStrategy
{
    string DocumentExtension { get; }
    Task<Document> ProcessAsync(IFormFile file);
}
