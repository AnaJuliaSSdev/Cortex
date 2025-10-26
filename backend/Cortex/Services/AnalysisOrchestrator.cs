using Cortex.Data;
using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class AnalysisOrchestrator(IAnalysisRepository analysisRepository, 
    StageStrategyFactory stageStrategyFactory, IStageRepository stageRepository, AppDbContext context) : IAnalysisOrchestrator
{
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;
    private readonly StageStrategyFactory _stageStrategyFactory = stageStrategyFactory;
    private readonly IStageRepository _stageRepository = stageRepository;
    private readonly AppDbContext _context = context;

    public async Task<AnalysisExecutionResult> StartAnalysisAsync(int analysisId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            Analysis? analysis = await _analysisRepository.GetByIdAsync(analysisId);

            if (analysis == null || analysis.UserId != userId)
                throw new EntityNotFoundException("Analysis");

            if (analysis.Status != AnalysisStatus.Draft)
                throw new InvalidOperationException("Analysis can only be started from Draft status");

            analysis.Status = AnalysisStatus.Running;

            Analysis updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await ContinueAnalysisAsync(updatedAnalysis!);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AnalysisExecutionResult> ContinueAnalysisAsync(Analysis analysis)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (analysis.Status == AnalysisStatus.Completed)
                throw new AnalysisAlreadyCompletedException();

            var lastStage = analysis.Stages.Count == 0 ? null : analysis.Stages.Last(); // pega o último stage adicionado, sem contexto, para ser executado
            var currentStageStrategy = _stageStrategyFactory.GetStrategy(lastStage);
            var resultcurrentStage = await currentStageStrategy.ExecuteStageAsync(analysis); // executa o stage e guarda o contexto nesse stage

            //aqui verificar se n deu nenhum erro antes de continuar

            var nextStage = FindNextStageStrategyFactory.GetNextStage(lastStage);
            if (nextStage is null)
                analysis.Status = AnalysisStatus.Completed;

            await _analysisRepository.UpdateAsync(analysis);
            await transaction.CommitAsync();

            return resultcurrentStage;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
