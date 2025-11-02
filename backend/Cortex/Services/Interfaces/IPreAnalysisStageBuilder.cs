using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IPreAnalysisStageBuilder
{
    Task<List<Models.Index>> BuildIndexesAsync(
        GeminiIndexResponse geminiResponse,
        PreAnalysisStage preAnalysisStage,
        IEnumerable<Cortex.Models.Document> allDocuments);
}
