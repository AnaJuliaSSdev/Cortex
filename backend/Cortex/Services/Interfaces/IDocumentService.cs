using Cortex.Models;
using Cortex.Models.DTO;
using GenerativeAI.Types;

namespace Cortex.Services.Interfaces;

public interface IDocumentService
{
    Task<Cortex.Models.Document> UploadAsync(CreateDocumentDto createDocumentDto, int analysisId);
    Task<Cortex.Models.Document?> GetByIdAsync(int id);
    Task<List<Part>> ConvertDocumentsToPart(IEnumerable<Cortex.Models.Document> allDocuments);
}
