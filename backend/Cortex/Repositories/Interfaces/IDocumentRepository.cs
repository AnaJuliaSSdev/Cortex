using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document);
    Task<Document?> GetByIdAsync(int id);
    Task<IEnumerable<Document>> GetByAnalysisIdAsync(int analysisId);
}
