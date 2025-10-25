using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

/// <summary>
/// Serviço responsável pela persistência de dados do PreAnalysisStage no banco de dados.
/// Gerencia transações de salvamento do Stage e seus Indexes relacionados.
/// </summary>
public class PreAnalysisPersistenceService : IPreAnalysisPersistenceService
{
    private readonly IStageRepository _stageRepository;
    private readonly IIndexRepository _indexRepository;
    private readonly ILogger<PreAnalysisPersistenceService> _logger;

    public PreAnalysisPersistenceService(
        IStageRepository stageRepository,
        IIndexRepository indexRepository,
        ILogger<PreAnalysisPersistenceService> logger)
    {
        _stageRepository = stageRepository;
        _indexRepository = indexRepository;
        _logger = logger;
    }

    /// <summary>
    /// Salva o PreAnalysisStage e todos os seus Indexes no banco de dados.
    /// Executa as operações em sequência: primeiro o Stage, depois os Indexes.
    /// </summary>
    /// <param name="analysisId">ID da análise pai</param>
    /// <param name="indexes">Lista de índices a serem persistidos</param>
    /// <returns>Entidade PreAnalysisStage salva com ID gerado</returns>
    public async Task<PreAnalysisStage> SavePreAnalysisAsync(
        int analysisId)
    {
        _logger.LogInformation("Iniciando persistência do PreAnalysisStage para Analysis ID: {AnalysisId}...", analysisId);

        PreAnalysisStage stageEntity = new()
        {
            AnalysisId = analysisId
        };
        _logger.LogDebug("Salvando entidade PreAnalysisStage no banco...");
        Stage savedStage = await _stageRepository.AddAsync(stageEntity);
        return (PreAnalysisStage)savedStage;
    }

    /// <summary>
    /// Salva todos os Indexes no banco de dados.
    /// IMPORTANTE: O PreAnalysisStageId já deve estar preenchido em cada Index.
    /// </summary>
    /// <param name="indexes">Lista de índices a serem persistidos</param>
    /// <param name="stageId">ID do PreAnalysisStage (para logging e validação)</param>
    /// <returns>Quantidade de índices salvos com sucesso</returns>
    public async Task<int> SaveIndexesAsync(List<Models.Index> indexes, int stageId)
    {
        _logger.LogInformation("Salvando {Count} índices para PreAnalysisStage ID: {StageId}...",
            indexes.Count, stageId);

        foreach (var index in indexes)
        {
            await _indexRepository.AddAsync(index);
        }

        return indexes.Count;
    }
}
