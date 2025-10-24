using Cortex.Exceptions;
using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using Google.Cloud.AIPlatform.V1;

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

    public List<Part> CreateVertexAiPartsFromDocuments(IEnumerable<Cortex.Models.Document> documents)
    {
        var fileParts = new List<Google.Cloud.AIPlatform.V1.Part>();

        foreach (var document in documents)
        {
            if (string.IsNullOrEmpty(document.GcsFilePath) || !document.GcsFilePath.StartsWith("gs://"))
            {               
                continue;
            }

            fileParts.Add(new Google.Cloud.AIPlatform.V1.Part
            {
                FileData = new FileData
                {
                    FileUri = document.GcsFilePath, // O GCS URI salvo no banco
                    MimeType = document.FileType.ToMimeType()
                }
            });
        }
        return fileParts;
    }
}