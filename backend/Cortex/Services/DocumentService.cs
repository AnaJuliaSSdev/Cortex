using Cortex.Exceptions;
using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class DocumentService(IDocumentRepository repository, ILogger<DocumentService> logger,
    IFileStorageService fileStorageService, IDocumentProcessingEmbeddingsService documentProcessingEmbeddingsService) : IDocumentService
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly ILogger _logger = logger;
    private readonly IDocumentProcessingEmbeddingsService _documentProcessingEmbeddingsService = documentProcessingEmbeddingsService;

    public async Task<Cortex.Models.Document> UploadAsync(CreateDocumentDto dto, int analysisId)
    {
        IDocumentProcessingStrategy? strategy = DocumentProcessingStrategyFactory.GetStrategy(dto.File);
        Models.Document? document = await strategy.ProcessAsync(dto.File);

        document.Title = dto.Title;
        document.Source = dto.Source;
        document.Purpose = dto.Purpose;
        document.AnalysisId = analysisId;
        document.CreatedAt = DateTime.UtcNow;

        FileStorageResult filePaths = await _fileStorageService.SaveFileAsync(dto.File, analysisId, strategy.DocumentExtension);
        document.FilePath = filePaths.LocalPath; // caminho para o diretório 
        document.GcsFilePath = filePaths.GcsPath; // caminho para o diretório do vertex
        document.FileSize = dto.File.Length;

        await _repository.AddAsync(document);
        //por enquanto sem gerar embeddings, analisando se vai ser necessário usar
        //await _documentProcessingEmbeddingsService.ProcessAsync(document, analysisId);

        return document;
    }

    public async Task<Cortex.Models.Document?> GetByIdAsync(int id)
    {
        Models.Document? document = await _repository.GetByIdAsync(id) ?? throw new EntityNotFoundException("Document");
        return document;
    }

    /// <summary>
    /// Mapeia uma lista de Documents para objetos DocumentInfo
    /// </summary>
    /// <param name="allDocuments"></param>
    /// <returns>Lista de DocumentInfo para usar no serviço do VERTEX</returns>
    /// <exception cref="AnalysisWithoutDocumentException"></exception>
    public List<DocumentInfo> MapDocumentsToDocumentsInfo(IEnumerable<Document> allDocuments)
    {

        var documentInfos = new List<DocumentInfo>();
        foreach (var doc in allDocuments)
        {
            // Usamos a propriedade GcsFilePath que você confirmou ter adicionado
            if (string.IsNullOrEmpty(doc.GcsFilePath) || !doc.GcsFilePath.StartsWith("gs://"))
            {
                _logger.LogWarning("Documento ID {DocId} ('{Title}') está sem GcsFilePath. Pulando.", doc.Id, doc.Title);
                continue;
            }

            documentInfos.Add(new DocumentInfo
            {
                GcsUri = doc.GcsFilePath,
                MimeType = doc.FileType.ToMimeType() // Assumindo que seu enum FileType tem este método helper
            });
        }

        if (documentInfos.Count == 0)
        {
            _logger.LogError("Nenhum documento com GCS URI válido foi encontrado para a análise. Abortando.");
            throw new AnalysisWithoutDocumentException();
        }

        return documentInfos;
    }
}