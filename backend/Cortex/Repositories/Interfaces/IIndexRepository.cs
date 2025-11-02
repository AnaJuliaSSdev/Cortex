using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IIndexRepository
{
    /// <summary>
    /// Adiciona um novo index ao banco de dados.
    /// </summary>
    /// <param name="index">O index a ser adicionado</param>
    Task AddAsync(Cortex.Models.Index index);

    Task<List<Models.Index>> GetByIdsAsync(List<int> ids);
    List<Models.Index> GetAll();
    Task<Models.Index?> GetByIdAsync(int id);
    Task<Models.Index?> GetByIdAAndUserIdsync(int id, int userId);
    Task<Models.Index> UpdateIndexAsync(Models.Index index);
    Task DeleteIndexAsync(Models.Index index);
}
