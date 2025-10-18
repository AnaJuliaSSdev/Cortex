using Cortex.Models;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Factories;

public class StageStrategyFactory(
   PreAnalysisStageService preAnalysisStageService,
   ExplorationOfMaterialStageService explorationOfMaterialStageService,
   InferenceConclusionStageService inferenceConclusionStageService)
{
    private readonly PreAnalysisStageService _preAnalysisStageService = preAnalysisStageService;
    private readonly ExplorationOfMaterialStageService _explorationOfMaterialStageService = explorationOfMaterialStageService;
    private readonly InferenceConclusionStageService _inferenceConclusionStageService = inferenceConclusionStageService;

    public AStageService GetStrategy(Stage stage)
    {
        return stage switch
        {
            PreAnalysisStage => _preAnalysisStageService,
            ExplorationOfMaterialStage => _explorationOfMaterialStageService,
            InferenceConclusionStage => _inferenceConclusionStageService,
            _ => throw new NotSupportedException($"No strategy found for type {stage.GetType().Name}")
        };
    }
}
