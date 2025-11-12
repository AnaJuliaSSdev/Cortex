using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class ExplorationPersistenceService(IStageRepository stageRepository, IIndexRepository indexRepository, 
    ILogger<ExplorationPersistenceService> logger) : IExplorationPersistenceService
{
    private readonly IStageRepository _stageRepository = stageRepository;
    private readonly IIndexRepository _indexRepository = indexRepository;
    private readonly ILogger<ExplorationPersistenceService> _logger = logger;


    /// <summary>
    /// Mapeia e salva a ExplorationOfMaterialStage completa.
    /// </summary>
    public async Task<ExplorationOfMaterialStage> MapAndSaveExplorationResultAsync(
        int analysisId,
        GeminiCategoryResponse geminiResponse,
        IEnumerable<Document> allDocuments)
    {
        _logger.LogInformation("Iniciando mapeamento e persistência da ExplorationOfMaterialStage para Analysis ID: {AnalysisId}...", analysisId);

        if (geminiResponse == null || geminiResponse.Categories == null || !geminiResponse.Categories.Any())
        {
            _logger.LogWarning("Resposta do Gemini não contém categorias válidas para Analysis ID: {AnalysisId}.", analysisId);
            // Decide se retorna nulo, lança exceção ou cria uma etapa vazia
            // Vamos criar uma etapa vazia por enquanto
            var emptyStage = new ExplorationOfMaterialStage { AnalysisId = analysisId };
            await _stageRepository.AddAsync(emptyStage); // Salva a etapa vazia
            return emptyStage;
            // throw new InvalidOperationException("A resposta do Gemini não continha categorias para processar.");
        }

        // Cria a entidade Stage "pai" em memória (sem salvar ainda)
        var stageEntity = new ExplorationOfMaterialStage
        {
            AnalysisId = analysisId,
            Categories = new List<Category>() // Inicializa a coleção
        };

        // Itera e Mapeia DTOs para Entidades (em memória)
        foreach (var geminiCategory in geminiResponse.Categories)
        {
            var newCategory = new Category
            {
                Name = geminiCategory.Name,
                Definition = geminiCategory.Definition,
                Frequency = geminiCategory.Frequency,
                ExplorationOfMaterialStage = stageEntity, // Associa ao pai (EF Core usará isso para FK)
                RegisterUnits = new List<RegisterUnit>() // Inicializa a coleção
            };

            if (geminiCategory.RegisterUnits != null)
            {
                foreach (var geminiUnit in geminiCategory.RegisterUnits)
                {
                    var newUnit = new RegisterUnit
                    {
                        Text = geminiUnit.Text,
                        SourceDocumentUri = FindGcsUriFromFileName(allDocuments, geminiUnit.Document), // Usa método auxiliar
                        Page = geminiUnit.Page,
                        Justification = geminiUnit.Justification,
                        Category = newCategory // Associa ao pai (EF Core usará isso para FK)
                        // FoundIndices será populado abaixo
                    };

                    // --- Associação M-M ---
                    if (geminiUnit.FoundIndices != null && geminiUnit.FoundIndices.Any())
                    {
                        var indexIds = geminiUnit.FoundIndices
                            .Select(idStr => int.TryParse(idStr, out int id) ? id : (int?)null)
                            .Where(id => id.HasValue)
                            .Select(id => id.Value)
                            .Distinct() // Evita buscar o mesmo ID múltiplas vezes
                            .ToList();

                        if (indexIds.Count != 0)
                        {
                            // Busca os Indexes existentes no banco DE UMA VEZ
                            var existingIndexes = await _indexRepository.GetByIdsAsync(indexIds);

                            // Adiciona as entidades Index encontradas à coleção da RegisterUnit
                            newUnit.FoundIndices = existingIndexes;
                        }
                        else
                        {
                            _logger.LogWarning("FoundIndices para RegisterUnit continha strings não reconhecidas: {IndexStrings}", string.Join(",", geminiUnit.FoundIndices));
                        }
                    }
                    // --- Fim Associação M-M ---

                    newCategory.RegisterUnits.Add(newUnit); // Adiciona a unidade à coleção da categoria
                }
            }
            stageEntity.Categories.Add(newCategory); // Adiciona a categoria (com suas unidades) à coleção da etapa
        }

        // Salva o Grafo Completo
        // Adiciona a entidade Stage raiz ao contexto. O EF Core Change Tracker
        // detectará todas as entidades relacionadas (Categories, RegisterUnits)
        // e as inserirá no banco na ordem correta, gerenciando as FKs e a tabela de junção M-M.
        _logger.LogInformation("Adicionando grafo completo da ExplorationOfMaterialStage (ID: {StageId}) ao contexto...", stageEntity.Id); // ID será 0 aqui
        await _stageRepository.AddAsync(stageEntity); // Assumindo que AddAsync chama _context.Add(stageEntity) e _context.SaveChangesAsync()

        // Retorna a entidade salva
        return stageEntity;
    }


    // Método auxiliar (movido ou duplicado da outra classe de serviço)
    private string FindGcsUriFromFileName(IEnumerable<Document> allDocuments, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || allDocuments == null) return $"NOME INVÁLIDO OU DOCUMENTOS NULOS: {fileName}";

        var doc = allDocuments.FirstOrDefault(d =>
            (d.FileName != null && d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)) ||
            (d.Title != null && d.Title.Equals(fileName, StringComparison.OrdinalIgnoreCase))
        );

        if (doc == null)
        {
            _logger.LogWarning("Não foi possível encontrar documento pelo nome/título: {FileName}", fileName);
            return $"NÃO ENCONTRADO: {fileName}";
        }
        if (string.IsNullOrEmpty(doc.GcsFilePath))
        {
            _logger.LogWarning("Documento encontrado (ID: {DocId}) mas GcsFilePath está vazio para FileName: {FileName}", doc.Id, fileName);
            return $"GCS PATH VAZIO: {fileName}";
        }

        return doc.GcsFilePath;
    }
}
