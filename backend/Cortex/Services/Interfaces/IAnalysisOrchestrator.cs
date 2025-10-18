using Cortex.Models;
using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IAnalysisOrchestrator
{
    Task<AnalysisExecutionResult> StartAnalysisAsync(int analysisId, int userId);
    Task<AnalysisExecutionResult> ContinueAnalysisAsync(Analysis analysis);
}