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
    StageStrategyFactory stageStrategyFactory, IStageRepository stageRepository, AppDbContext context,
    IUnitOfWork unitOfWork) : IAnalysisOrchestrator
{
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;
    private readonly StageStrategyFactory _stageStrategyFactory = stageStrategyFactory;
    private readonly IStageRepository _stageRepository = stageRepository;
    private readonly AppDbContext _context = context;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AnalysisExecutionResult> StartAnalysisAsync(int analysisId, int userId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            Analysis? analysis = await _analysisRepository.GetByIdAsync(analysisId);

            if (analysis == null || analysis.UserId != userId)
                throw new EntityNotFoundException("Analysis");

            if (analysis.Status != AnalysisStatus.Draft)
                throw new InvalidOperationException("Analysis can only be started from Draft status");

            analysis.Status = AnalysisStatus.Running;

            Analysis updatedAnalysis = await _analysisRepository.UpdateAsync(analysis);

            await _unitOfWork.CommitTransactionAsync();

            return await ContinueAnalysisAsync(updatedAnalysis!);
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<AnalysisExecutionResult> ContinueAnalysisAsync(Analysis analysis)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (analysis.Status == AnalysisStatus.Completed)
                throw new AnalysisAlreadyCompletedException();

            var lastStage = analysis.Stages.Count == 0 ? null : analysis.Stages.Last(); // pega o último stage adicionado, sem contexto, para ser executado
            var currentStageStrategy = _stageStrategyFactory.GetStrategy(lastStage);
            var resultcurrentStage = await currentStageStrategy.ExecuteStageAsync(analysis); // executa o stage e guarda o contexto nesse stage

            var nextStage = FindNextStageStrategyFactory.GetNextStage(currentStageStrategy);
            if (nextStage is null)
                analysis.Status = AnalysisStatus.Completed;

            await _analysisRepository.UpdateAsync(analysis);
            await _unitOfWork.CommitTransactionAsync();

            return resultcurrentStage;
        }
        catch (Exception ex)
        {
            // Cancela a transação atual
            await _unitOfWork.RollbackTransactionAsync();

            try
            {
                var analysisToFail = await _analysisRepository.GetByIdAsync(analysis.Id);

                if(analysisToFail != null)
                {
                    await _analysisRepository.RevertLastStageAsync(analysis.Id);
                    await _unitOfWork.SaveChangesAsync();
                }                             
            }
            catch (Exception rollbackEx)
            {
                throw new AggregateException("Falha na operação E no rollback subsequente.", ex, rollbackEx);
            }

            throw;
        }
    }
}
