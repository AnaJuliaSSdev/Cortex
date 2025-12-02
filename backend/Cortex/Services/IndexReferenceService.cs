using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class IndexReferenceService(ILogger<IndexReferenceService> logger) : IIndexReferenceService
{
    private readonly ILogger _logger = logger;

    public IndexReference MapGeminiRefToIndexReference(GeminiReference geminiReference, Models.Index newIndex, IEnumerable<Cortex.Models.Document> allDocuments)
    {
        // Encontra o GCS URI correspondente ao nome do arquivo
        string targetName = Path.GetFileNameWithoutExtension(geminiReference.Document)?.Trim().ToLower() ?? "";
        var foundDoc = allDocuments.FirstOrDefault(d =>
        {
            string dbDocName = Path.GetFileNameWithoutExtension(d.FileName)?.Trim().ToLower() ?? "";

            // COMPARAÇÃO FLEXÍVEL:
            // Verifica se um contém o outro. Isso resolve casos onde:
            // - O Gemini mandou "Relatorio" e o banco tem "Relatorio_Final"
            // - O Gemini mandou "Entrevista.txt" e o banco tem "Entrevista.pdf"
            return dbDocName == targetName ||
                   dbDocName.Contains(targetName) ||
                   targetName.Contains(dbDocName);
        });

        string gcsUri;
        if (foundDoc != null)
        {
            gcsUri = foundDoc.GcsFilePath; // Pega o caminho real do documento encontrado
        }
        else
        {
            // Se ainda assim não achar, mantemos o log de aviso
            _logger.LogWarning("Não foi possível encontrar o GCS URI para: {DocName}. Tentativa normalizada: {TargetName}", geminiReference.Document, targetName);
            gcsUri = $"NÃO ENCONTRADO: {geminiReference.Document}";
        }

        IndexReference newReference = new()
        {
            Index = newIndex, // EF Core associará automaticamente
            SourceDocumentUri = gcsUri,
            Page = geminiReference.Page,
            QuotedContent = geminiReference.QuotedContent
        };

        return newReference;
    }
}
