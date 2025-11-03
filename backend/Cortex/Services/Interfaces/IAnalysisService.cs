using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IAnalysisService
{
    Task<Analysis?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<AnalysisDto?>> GetByUserIdAsync(int userId);
    Task<AnalysisDto> CreateAsync(CreateAnalysisDto createDto, int userId);
    Task DeleteAsync(int id, int userId);
    Task<bool> PostAnalysisQuestion(int id, StartAnalysisDto startAnalysisDto, int userId);
    Task<AnalysisExecutionResult> GetFullStateByIdAsync(int analysisId, int userId);
    Task<PaginatedResultDto<AnalysisDto>> GetByUserIdPaginatedAsync(int userId, PaginationQueryDto paginationParams);
    Task DeleteDocumentAsync(int documentId, int userId);
}
