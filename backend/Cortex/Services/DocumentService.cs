using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using Pgvector;

namespace Cortex.Services;

public class DocumentService(IDocumentRepository repository,
    IFileStorageService fileStorageService, IDocumentProcessingEmbeddingsService documentProcessingEmbeddingsService) : IDocumentService
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IDocumentProcessingEmbeddingsService _documentProcessingEmbeddingsService = documentProcessingEmbeddingsService;

    public async Task<Document> UploadAsync(CreateDocumentDto dto, int analysisId)
    {
        IDocumentProcessingStrategy? strategy = DocumentProcessingStrategyFactory.GetStrategy(dto.File);
        Document? document = await strategy.ProcessAsync(dto.File);

        document.Title = dto.Title;
        document.Source = dto.Source;
        document.Purpose = dto.Purpose;
        document.AnalysisId = analysisId;
        document.CreatedAt = DateTime.UtcNow;

        string filePath = await _fileStorageService.SaveFileAsync(dto.File, analysisId, strategy.DocumentExtension);
        document.FilePath = filePath;
        document.FileSize = dto.File.Length;

        await _repository.AddAsync(document);

        await _documentProcessingEmbeddingsService.ProcessAsync(document, analysisId);

        return document;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        Document? document = await _repository.GetByIdAsync(id) ?? throw new EntityNotFoundException("Document");
        return document;
    }
}