using Cortex.Exceptions;
using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using GenerativeAI.Types;

namespace Cortex.Services;

public class DocumentService(IDocumentRepository repository,
    IFileStorageService fileStorageService, IDocumentProcessingEmbeddingsService documentProcessingEmbeddingsService) : IDocumentService
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
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

        string filePath = await _fileStorageService.SaveFileAsync(dto.File, analysisId, strategy.DocumentExtension);
        document.FilePath = filePath;
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

    public async Task<List<Part>> ConvertDocumentsToPart(IEnumerable<Cortex.Models.Document> allDocuments)
    {
        List<Part> fileParts = [];
        foreach (Cortex.Models.Document document in allDocuments)
        {

            byte[] fileBytes = await _fileStorageService.GetFileAsync(document.FilePath);
            var base64Data = Convert.ToBase64String(fileBytes);

            fileParts.Add(new Part
            {
                InlineData = new Blob
                {
                    MimeType = document.FileType.ToMimeType(),
                    Data = base64Data
                }
            });
        }

        return fileParts;
    }
}