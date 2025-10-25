using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IIndexReferenceService
{
    IndexReference MapGeminiRefToIndexReference(GeminiReference geminiReference, Models.Index newIndex, IEnumerable<Cortex.Models.Document> allDocuments);
}
