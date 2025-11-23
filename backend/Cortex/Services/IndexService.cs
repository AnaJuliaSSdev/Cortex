using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Index = Cortex.Models.Index;

namespace Cortex.Services;

public class IndexService(IStageRepository stageRepository, IIndicatorService indicatorService, 
    IIndexRepository indexRepository, IUnitOfWork unitOfWork) : IIndexService
{
    public readonly IStageRepository _stageRepository = stageRepository;
    private readonly IIndicatorService _indicatorService = indicatorService;
    private readonly IIndexRepository _indexRepository = indexRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Models.Index> CreateManualIndexAsync(int userId, CreateManualIndexDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();
        // Verifica se o usuário é dono da PreAnalysisStage
        var stage = await _stageRepository.GetByIdAndUserIdAsync(dto.PreAnalysisStageId, userId) ?? throw new StageDontBelongToUserException();

        //Encontra ou Crie o Indicador
        Indicator indicator = await _indicatorService.GetOrCreateIndicatorAsync(dto.IndicatorName);

        //Cria o novo index
        var newIndex = new Models.Index
        {
            Name = dto.IndexName,
            Description = dto.IndexDescription,
            Indicator = indicator, // Linka o indicador encontrado ou criado
            PreAnalysisStageId = dto.PreAnalysisStageId
        };

        await _indexRepository.AddAsync(newIndex);

        await _unitOfWork.CommitTransactionAsync();
        return newIndex;
    }

    public async Task DeleteIndexAsync(int indexId, int userId)
    {
        await _unitOfWork.BeginTransactionAsync();
        Index? index = await _indexRepository.GetByIdAAndUserIdsync(indexId, userId) ?? throw new EntityNotFoundException(typeof(Index).ToString());       
        await _indexRepository.DeleteIndexAsync(index);
        await _unitOfWork.CommitTransactionAsync();
    }

    public async Task<Models.Index> UpdateIndexAsync(int indexId, int userId, UpdateIndexDto indexUpdate)
    {
        await _unitOfWork.BeginTransactionAsync();
        var index = await _indexRepository.GetByIdAAndUserIdsync(indexId, userId) ?? throw new EntityNotFoundException(typeof(Index).ToString());

        // Lógica para encontrar ou criar o Indicador (similar à de criação)
        //Encontra ou Crie o Indicador
        Indicator indicator = await _indicatorService.GetOrCreateIndicatorAsync(indexUpdate.IndicatorName);

        // Atualiza o índice
        index.Name = indexUpdate.IndexName;
        index.Description = indexUpdate.IndexDescription;
        index.Indicator = indicator; // Linka o indicador

        //salva e recarrega indice pra vir o indicador junto
        var updatedIndex = await _indexRepository.UpdateIndexAsync(index);

        await _unitOfWork.CommitTransactionAsync();
        return updatedIndex;
    }
}
