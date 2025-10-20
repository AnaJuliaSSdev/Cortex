using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class ExplorationOfMaterialStageService(IDocumentRepository documentRepository) : AStageService(documentRepository)
{
    public Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
    {
        throw new NotImplementedException();
    }

    public override string GetPromptStageAsync()
    {
        throw new NotImplementedException();
    }
}
