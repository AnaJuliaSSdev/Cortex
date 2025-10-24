using Cortex.Models;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class IndicatorService(IIndicatorRepository indicatorRepository) : IIndicatorService
{
    private readonly IIndicatorRepository _indicatorRepository = indicatorRepository;

    /// <summary>
    /// Busca um Indicador pelo nome. Se não existir, cria um novo.
    /// </summary>
    public async Task<Indicator> GetOrCreateIndicatorAsync(string indicatorName)
    {
        if (string.IsNullOrWhiteSpace(indicatorName))
        {
            indicatorName = "Não especificado";
        }

        // (Assumindo que seu repositório tem um método 'GetByNameAsync')
        Indicator? existingIndicator = await _indicatorRepository.GetByNameAsync(indicatorName);
        if (existingIndicator != null)
        {
            return existingIndicator;
        }

        // Criar um novo se não existir
        Indicator newIndicator = new() { Name = indicatorName };
        return await _indicatorRepository.AddAsync(newIndicator);
    }
}
