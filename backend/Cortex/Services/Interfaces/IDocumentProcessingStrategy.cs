using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IDocumentProcessingStrategy
{
    string DocumentExtension { get; }
    Task<ProcessedDocumentResult> ProcessAsync(IFormFile file);
}
