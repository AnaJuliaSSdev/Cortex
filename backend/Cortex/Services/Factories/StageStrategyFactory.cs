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
            null => _preAnalysisStageService,
            PreAnalysisStage => _explorationOfMaterialStageService,
            ExplorationOfMaterialStage => _inferenceConclusionStageService,
            //InferenceConclusionStage => aqui teoricamente já acabou
            _ => throw new NotSupportedException($"No strategy found for type {stage.GetType().Name}")
        };
    }
}
