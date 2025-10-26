using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class InferenceConclusionStageService(IDocumentRepository documentRepository) : AStageService(documentRepository)
{
    public override string FormatStagePromptAsync(Analysis analysis, AnalysisExecutionResult resultBaseClass, object? previousStageData = null)
    {
        throw new NotImplementedException();
    }

    public override string GetStagePromptTemplate()
    {
        throw new NotImplementedException();
    }
}
