using Cortex.Models;

namespace Cortex.Services.Factories;

public static class FindNextStageStrategyFactory
{
    public static Stage? GetNextStage(Stage currentStage)
    {
        return currentStage switch
        {
            null => new PreAnalysisStage(),
            PreAnalysisStage => new ExplorationOfMaterialStage(),
            ExplorationOfMaterialStage => new InferenceConclusionStage(),
            InferenceConclusionStage => null, 
            _ => throw new NotSupportedException($"Unrecognized stage type: {currentStage.GetType().Name}")
        };
    }
}
