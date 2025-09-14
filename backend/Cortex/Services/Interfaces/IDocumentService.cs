using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IDocumentService
{
    Task<Document> UploadAsync(CreateDocumentDto createDocumentDto, int analysisId);
    Task<Document?> GetByIdAsync(int id);
}
