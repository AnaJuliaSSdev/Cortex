using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IAnalysisService
{
    Task<Analysis?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<AnalysisDto?>> GetByUserIdAsync(int userId);
    Task<AnalysisDto> CreateAsync(CreateAnalysisDto createDto, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}
