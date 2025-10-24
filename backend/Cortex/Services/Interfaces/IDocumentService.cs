using Cortex.Models;
using Cortex.Models.DTO;
using Google.Cloud.AIPlatform.V1;

namespace Cortex.Services.Interfaces;

public interface IDocumentService
{
    Task<Cortex.Models.Document> UploadAsync(CreateDocumentDto createDocumentDto, int analysisId);
    Task<Cortex.Models.Document?> GetByIdAsync(int id);
    List<Part> CreateVertexAiPartsFromDocuments(IEnumerable<Cortex.Models.Document> documents);
}
