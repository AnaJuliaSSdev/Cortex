using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using Pgvector;

namespace Cortex.Services;

public class DocumentService(DocumentProcessingStrategyFactory factory, 
    IDocumentRepository repository, IFileStorageService fileStorageService, IChunkService chunkService, IChunkRepository chunkRepository, IEmbeddingService embeddingService) : IDocumentService
{
    private readonly DocumentProcessingStrategyFactory _factory = factory;
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IChunkService _chunkService = chunkService;
    private readonly IChunkRepository _chunkRepository = chunkRepository;
    private readonly IEmbeddingService _embeddingService = embeddingService;


    public async Task<Document> UploadAsync(CreateDocumentDto dto, int analysisId)
    {
        IDocumentProcessingStrategy? strategy = _factory.GetStrategy(dto.File);

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

        var chunksTexts = _chunkService.SplitIntoChunks(document.Content ?? "");
        var chunks = new List<Chunk>();

        var embeddingTasks = chunksTexts.Select(chunkText => _embeddingService.GenerateEmbeddingAsync(chunkText)).ToList();

        var embeddings = await Task.WhenAll(embeddingTasks);

        for (int i = 0; i < chunksTexts.Count; i++)
        {
            var chunk = new Chunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                Content = chunksTexts[i],
                Embedding = new Vector(embeddings[i]),
                TokenCount = chunksTexts[i].Length
            };
            chunks.Add(chunk);
        }

        if (chunks.Count > 0)
            await _chunkRepository.AddRangeAsync(chunks);

        return document;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        Document? document = await _repository.GetByIdAsync(id) ?? throw new EntityNotFoundException("Document");
        return document;
    }
}