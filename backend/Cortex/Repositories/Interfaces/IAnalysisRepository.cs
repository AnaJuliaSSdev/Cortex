using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IAnalysisRepository
{
    Task<Analysis?> GetByIdAsync(int id);
    Task<Analysis?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Analysis>> GetByUserIdAsync(int userId);
    Task<Analysis> CreateAsync(Analysis analysis);
    Task<Analysis> UpdateAsync(Analysis analysis);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> BelongsToUserAsync(int analysisId, int userId);
}
