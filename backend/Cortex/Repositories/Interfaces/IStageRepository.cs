using Cortex.Models;

namespace Cortex.Repositories.Interfaces
{
    public interface IStageRepository
    {
        Task<Stage> AddAsync(Stage stage);
        Task<Stage?> GetByIdAndUserIdAsync(int stageId, int userId);
    }
}
