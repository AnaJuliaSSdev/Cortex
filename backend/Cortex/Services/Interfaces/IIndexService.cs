using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IIndexService
{
    Task<Cortex.Models.Index> CreateManualIndexAsync(int userId, CreateManualIndexDto dto);
    Task<Cortex.Models.Index> UpdateIndexAsync(int indexId, int userId, UpdateIndexDto indexUpdate);
    Task DeleteIndexAsync(int indexId, int userId);
}
