using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Pgvector;

namespace Cortex.Services;

public class DocumentProcessingEmbeddingsService(
    IChunkService chunkService,
    IChunkRepository chunkRepository,
    IEmbeddingService embeddingService,
    IAnalysisRepository analysisRepository) : IDocumentProcessingEmbeddingsService
{
    private readonly IChunkService _chunkService = chunkService;
    private readonly IChunkRepository _chunkRepository = chunkRepository;
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;

    public async Task ProcessAsync(Document document, int analysisId)
    {
        //Analysis documents don't need chunks or embeddings
        if (document.Purpose != Models.Enums.DocumentPurpose.Reference)
            return;

        var chunksTexts = _chunkService.SplitIntoChunks(document.Content ?? "");

        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunksTexts);

        if (embeddings.Count != chunksTexts.Count)
            throw new FailedToGenerateEmbeddingsException();

        Analysis? analysis = await _analysisRepository.GetByIdAsync(analysisId) ?? throw new EntityNotFoundException(nameof(analysisId));

        var relevantChunks = chunksTexts
            .Zip(embeddings, (text, emb) => new { text, emb })
            .Where(pair => embeddings.Contains(pair.emb))
            .Select((pair, i) => new Chunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                Content = pair.text,
                Embedding = new Vector(pair.emb),
                TokenCount = pair.text.Length
            })
            .ToList();

        if (relevantChunks.Count > 0)
            await _chunkRepository.AddRangeAsync(relevantChunks);
    }
}
