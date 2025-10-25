using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using Mscc.GenerativeAI;

namespace Cortex.Services;

public class IndexReferenceService(ILogger<IndexReferenceService> logger) : IIndexReferenceService
{
    private readonly ILogger _logger = logger;

    public IndexReference MapGeminiRefToIndexReference(GeminiReference geminiReference, Models.Index newIndex, IEnumerable<Cortex.Models.Document> allDocuments)
    {
        // Encontra o GCS URI correspondente ao nome do arquivo
        string gcsUri = Util.Util.FindGcsUriFromFileName(allDocuments, geminiReference.Document);
        if (gcsUri == null)
        {
            _logger.LogWarning("Não foi possível encontrar o GCS URI para o documento de referência: {DocName}", geminiReference.Document);
            gcsUri = $"NÃO ENCONTRADO: {geminiReference.Document}";
        }

        IndexReference newReference = new()
        {
            Index = newIndex, // EF Core associará automaticamente
            SourceDocumentUri = gcsUri,
            Page = geminiReference.Page,
            Line = geminiReference.Line,
            // aqui teria que pedir pra ele gerar jutno o trecho exato, além das páginas e linhas
            QuotedContent = $"Pág: {geminiReference.Page}, Linha: {geminiReference.Line}"
        };

        return newReference;
    }
}
