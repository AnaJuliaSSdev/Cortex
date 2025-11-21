using Cortex.Exceptions;
using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Services;

public class DocumentService(IDocumentRepository repository, ILogger<DocumentService> logger,
    IFileStorageService fileStorageService) : IDocumentService
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly ILogger _logger = logger;
    const long MAX_TOTAL_SIZE_BYTES = 100 * 1024 * 1024; // 100MB

    public async Task<Cortex.Models.Document> UploadAsync(CreateDocumentDto dto, int analysisId)
    {
        ValidateFileMaxSize(dto, analysisId);

        IDocumentProcessingStrategy? strategy = DocumentProcessingStrategyFactory.GetStrategy(dto.File);
        ProcessedDocumentResult result = await strategy.ProcessAsync(dto.File);
        Models.Document document = result.DocumentModel;

        document.Title = dto.Title;
        document.Source = dto.Source;
        document.Purpose = dto.Purpose;
        document.AnalysisId = analysisId;
        document.CreatedAt = DateTime.UtcNow;

        string gcsPath = await _fileStorageService.SaveFileAsync(
                    result.FileStream,
                    result.DocumentModel.FileName, // Usa o nome que veio da estratégia (já com .pdf se foi convertido)
                    result.ContentType,
                    analysisId
                );

        document.FileType = result.DocumentType;
        document.GcsFilePath = gcsPath;
        document.FilePath = null;
        document.FileSize = result.FileStream.Length;

        await _repository.AddAsync(document);
        result.FileStream.Dispose();

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
                MimeType = doc.FileType.ToMimeType(), // Assumindo que seu enum FileType tem este método helper
                FileName = doc.FileName, 
                Content = doc.Content
            });
        }

        if (documentInfos.Count == 0)
        {
            _logger.LogError("Nenhum documento com GCS URI válido foi encontrado para a análise. Abortando.");
            throw new AnalysisWithoutDocumentException();
        }

        return documentInfos;
    }

    private async void ValidateFileMaxSize(CreateDocumentDto dto, int analysisId)
    {
        //Tamanho do novo arquivo
        long newFileSize = dto.File.Length;

        // Calcule o tamanho dos arquivos existentes para esta análise E este propósito
        var existingSize = await repository.SumTotalSizeDocumentsByAnalysisIdAsync(analysisId, dto.Purpose);

        if (existingSize + newFileSize > MAX_TOTAL_SIZE_BYTES)
        {
            throw new ValidationException($"Limite de 100MB para documentos de '{dto.Purpose}' foi excedido.");
        }
    }
}