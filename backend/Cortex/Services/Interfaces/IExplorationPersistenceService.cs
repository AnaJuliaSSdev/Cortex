using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IExplorationPersistenceService
{
    /// <summary>
    /// Mapeia a resposta desserializada do Gemini para entidades do EF Core
    /// e salva a ExplorationOfMaterialStage completa (com Categories, RegisterUnits e associações)
    /// no banco de dados.
    /// </summary>
    /// <param name="analysisId">ID da Análise associada.</param>
    /// <param name="geminiResponse">A resposta desserializada do Gemini (contendo Categorias).</param>
    /// <param name="allDocuments">A lista de todos os documentos da análise (para encontrar URIs).</param>
    /// <returns>A entidade ExplorationOfMaterialStage salva e totalmente populada.</returns>
    /// <exception cref="InvalidOperationException">Se ocorrer um erro durante o salvamento ou busca de dados relacionados.</exception>
    Task<ExplorationOfMaterialStage> MapAndSaveExplorationResultAsync(
        int analysisId,
        GeminiCategoryResponse geminiResponse,
        IEnumerable<Document> allDocuments); // Passa os documentos para encontrar URIs
}
