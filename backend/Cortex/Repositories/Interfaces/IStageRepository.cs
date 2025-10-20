using Cortex.Models;

namespace Cortex.Repositories.Interfaces
{
    public interface IStageRepository
    {
        Task AddAsync(Stage stage);
    }
}
