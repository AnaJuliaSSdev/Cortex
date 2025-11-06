using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Cortex.Services.Strategies;
using Pgvector;

namespace Cortex.Services.ServicosTopicos
{
    public class SimplePdfProcessingService
    {
        private readonly IChunkService _chunkService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IChunkRepository _chunkRepository;
        private readonly ILogger<SimplePdfProcessingService> _logger;

        public SimplePdfProcessingService(
            IChunkService chunkService,
            IEmbeddingService embeddingService,
            IDocumentRepository documentRepository,
            IChunkRepository chunkRepository,
            ILogger<SimplePdfProcessingService> logger)
        {
            _chunkService = chunkService;
            _embeddingService = embeddingService;
            _documentRepository = documentRepository;
            _chunkRepository = chunkRepository;
            _logger = logger;
        }

        public async Task<Document> ProcessPdfAsync(string pdfPath, string title)
        {
            _logger.LogInformation("Processando PDF: {Path}", pdfPath);

            // 1. Ler o PDF
            var file = CreateFormFileFromPath(pdfPath);
            var strategy = new PdfDocumentProcessingStrategy();
            var document = await strategy.ProcessAsync(file);

            // 2. Salvar documento no banco
            document.Title = title;
            document.FilePath = pdfPath;
            document.GcsFilePath = ""; // Não usamos GCS
            document.CreatedAt = DateTime.UtcNow;
            document.Purpose = Models.Enums.DocumentPurpose.Reference;
            document.AnalysisId = 17; // Análise padrão para o console

            await _documentRepository.AddAsync(document);
            _logger.LogInformation("Documento salvo. ID: {Id}", document.Id);

            // 3. Dividir em chunks
            _logger.LogInformation("Dividindo em chunks...");
            var chunksTexts = _chunkService.SplitIntoChunks(document.Content ?? "", chunkSize: 1000, overlap: 200);
            _logger.LogInformation("Total de chunks: {Count}", chunksTexts.Count);

            // 4. Gerar embeddings
            _logger.LogInformation("Gerando embeddings...");
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunksTexts);

            // 5. Salvar chunks no banco
            _logger.LogInformation("Salvando chunks no banco...");
            var chunks = chunksTexts
                .Zip(embeddings, (text, emb) => new { text, emb })
                .Select((pair, i) => new Chunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = i,
                    Content = pair.text,
                    Embedding = new Vector(pair.emb),
                    TokenCount = pair.text.Length
                })
                .ToList();

            await _chunkRepository.AddRangeAsync(chunks);
            _logger.LogInformation("Processamento concluído!");

            return document;
        }

        private IFormFile CreateFormFileFromPath(string path)
        {
            var fileStream = File.OpenRead(path);
            var fileName = Path.GetFileName(path);

            return new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };
        }
    }
}
