using Cortex.Models;
using Cortex.Models.Enums;

namespace Cortex.Repositories.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document);
    Task<Document?> GetByIdAsync(int id);
    Task<Document?> GetByIdWithAnalysisAsync(int id); 
    Task<IEnumerable<Document>> GetByAnalysisIdAsync(int analysisId);
    Task<long> SumTotalSizeDocumentsByAnalysisIdAsync(int analysisId, DocumentPurpose documentPurpose);
    Task DeleteAsync(Document document);

}
