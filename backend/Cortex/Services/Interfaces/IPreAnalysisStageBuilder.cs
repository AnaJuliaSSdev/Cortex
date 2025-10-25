using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IPreAnalysisStageBuilder
{
    Task<List<Models.Index>> BuildIndexesAsync(
        GeminiIndexResponse geminiResponse,
        int preAnalysisStageId,
        IEnumerable<Cortex.Models.Document> allDocuments);
}
