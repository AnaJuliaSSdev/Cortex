using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IPreAnalysisPersistenceService
{
    Task<PreAnalysisStage> SavePreAnalysisAsync(
        int analysisId);

    Task<int> SaveIndexesAsync(List<Models.Index> indexes);

}
