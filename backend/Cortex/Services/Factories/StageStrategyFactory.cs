using Cortex.Models;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Factories;

public class StageStrategyFactory(
   PreAnalysisStageService preAnalysisStageService,
   ExplorationOfMaterialStageService explorationOfMaterialStageService)
{
    private readonly PreAnalysisStageService _preAnalysisStageService = preAnalysisStageService;
    private readonly ExplorationOfMaterialStageService _explorationOfMaterialStageService = explorationOfMaterialStageService;

    public AStageService GetStrategy(Stage stage)
    {
        return stage switch
        {
            null => _preAnalysisStageService,
            PreAnalysisStage => _explorationOfMaterialStageService,
            _ => throw new NotSupportedException($"No strategy found for type {stage.GetType().Name}")
        };
    }
}
