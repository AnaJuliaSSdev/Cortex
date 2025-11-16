using Cortex.Models;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Factories;

public static class FindNextStageStrategyFactory
{
    public static Stage? GetNextStage(AStageService currentStage)
    {
        return currentStage switch
        {
            null => new PreAnalysisStage(),
            PreAnalysisStageService => new ExplorationOfMaterialStage(),
            ExplorationOfMaterialStageService => null,
            _ => throw new NotSupportedException($"Unrecognized stage type: {currentStage.GetType().Name}")
        };
    }
}
