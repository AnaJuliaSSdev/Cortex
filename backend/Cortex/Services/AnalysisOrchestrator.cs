using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class AnalysisOrchestrator(IAnalysisRepository analysisRepository, StageStrategyFactory stageStrategyFactory) : IAnalysisOrchestrator
{
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;
    private readonly StageStrategyFactory _stageStrategyFactory = stageStrategyFactory;

    public async Task<AnalysisExecutionResult> StartAnalysisAsync(int analysisId, int userId)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);

        if (analysis == null || analysis.UserId != userId)
            throw new EntityNotFoundException("Analysis");

        if (analysis.Status != AnalysisStatus.Draft)
            throw new InvalidOperationException("Analysis can only be started from Draft status");

        analysis.Status = AnalysisStatus.Running;
        analysis.Stages.Add(new PreAnalysisStage());

        Analysis updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

        return await ContinueAnalysisAsync(updatedAnalysis!);
    }

    public async Task<AnalysisExecutionResult> ContinueAnalysisAsync(Analysis analysis)
    {
        if (analysis.Status == AnalysisStatus.Completed)
            throw new AnalysisAlreadyCompletedException();

        var lastStage = analysis.Stages.Last(); // pega o último stage adicionado, sem contexto, para ser executado
        var currentStageStrategy = _stageStrategyFactory.GetStrategy(lastStage);
        var resultcurrentStage = await currentStageStrategy.ExecuteStageAsync(analysis); // executa o stage e guarda o contexto nesse stage

        var nextStage = FindNextStageStrategyFactory.GetNextStage(lastStage);
        if (nextStage is not null)
            analysis.Stages.Add(nextStage); // adiciona o próximo stage a lista, sem o contexto
        else
            analysis.Status = AnalysisStatus.Completed; // caso não tenha próxima etapa a análise está completa

        await _analysisRepository.UpdateAsync(analysis);
        return resultcurrentStage;
    }
}
