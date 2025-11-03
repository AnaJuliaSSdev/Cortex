using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

/// <summary>
/// Serviço responsável por construir a estrutura de dados do PreAnalysisStage.
/// Mapeia a resposta do Gemini para entidades de domínio (Index, Indicator, References).
/// </summary>
public class PreAnalysisStageBuilder(
    IIndicatorService indicatorService,
    IIndexReferenceService indexReferenceService,
    ILogger<PreAnalysisStageBuilder> logger) : IPreAnalysisStageBuilder
{
    private readonly IIndicatorService _indicatorService = indicatorService;
    private readonly IIndexReferenceService _indexReferenceService = indexReferenceService;
    private readonly ILogger<PreAnalysisStageBuilder> _logger = logger;

    /// <summary>
    /// Constrói uma lista de entidades Index a partir da resposta do Gemini.
    /// Cada Index contém seus Indicators e References associados.
    /// </summary>
    /// <param name="geminiResponse">Resposta processada do Gemini contendo os índices</param>
    /// <param name="preAnalysisStage">PreAnalysisStage ao qual os índices pertencem</param>
    /// <param name="allDocuments">Todos os documentos da análise (referência + análise)</param>
    /// <returns>Lista de entidades Index prontas para persistência</returns>
    public async Task<List<Models.Index>> BuildIndexesAsync(
        GeminiIndexResponse geminiResponse,
        PreAnalysisStage preAnalysisStage,
        IEnumerable<Cortex.Models.Document> allDocuments)
    {
        _logger.LogInformation("Iniciando construção de {Count} índices...", geminiResponse.Indices.Count);
        var indexes = new List<Models.Index>();

        foreach (var geminiIndex in geminiResponse.Indices)
        {
            var index = await BuildSingleIndexAsync(geminiIndex, preAnalysisStage, allDocuments);
            indexes.Add(index);
        }

        return indexes;
    }

    /// <summary>
    /// Constrói uma única entidade Index com seus relacionamentos.
    /// Busca ou cria o Indicator e mapeia todas as References.
    /// </summary>
    /// <param name="geminiIndex">Índice individual da resposta do Gemini</param>
    /// <param name="preAnalysisStage">Stage PreAnalysisStage pai</param>
    /// <param name="allDocuments">Documentos para mapear as referências</param>
    /// <returns>Entidade Index completa</returns>
    private async Task<Models.Index> BuildSingleIndexAsync(
        GeminiIndex geminiIndex,
        PreAnalysisStage preAnalysisStage,
        IEnumerable<Cortex.Models.Document> allDocuments)
    {
        Indicator indicator = await _indicatorService.GetOrCreateIndicatorAsync(geminiIndex.Indicator);

        var newIndex = new Models.Index
        {
            Name = geminiIndex.Name,
            Description = geminiIndex.Description,
            Indicator = indicator,
            PreAnalysisStage = preAnalysisStage
        };

        if (geminiIndex.References != null)
        {
            _logger.LogDebug("Mapeando {Count} referências para o índice '{IndexName}'...",
               geminiIndex.References.Count, geminiIndex.Name);

            foreach (GeminiReference geminiRef in geminiIndex.References)
            {
                newIndex.References.Add(
                    _indexReferenceService.MapGeminiRefToIndexReference(geminiRef, newIndex, allDocuments)
                );
            }
        }

        return newIndex;
    }
}
