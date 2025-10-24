using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IIndicatorRepository
{
    /// <summary>
    /// Busca um indicador pelo seu nome único.
    /// </summary>
    /// <param name="name">O nome do indicador</param>
    /// <returns>A entidade Indicator ou null se não for encontrada.</returns>
    Task<Indicator?> GetByNameAsync(string name);

    /// <summary>
    /// Adiciona um novo indicador ao banco de dados.
    /// </summary>
    /// <param name="indicator">O indicador a ser adicionado</param>
    Task<Indicator> AddAsync(Indicator indicator);
}
