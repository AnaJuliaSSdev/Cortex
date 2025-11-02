using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using StockApp2._0.Mapper;
using Cortex.Exceptions;

namespace Cortex.Services;

public class AnalysisService(IAnalysisRepository analysisRepository) : IAnalysisService
{
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;

    public async Task<Analysis?> GetByIdAsync(int id, int userId)
    {
        if (!await _analysisRepository.BelongsToUserAsync(id, userId))
            throw new AnalysisDontBelongToUserException();

        Analysis? analysis = await _analysisRepository.GetByIdWithDetailsAsync(id);

        if (analysis == null || analysis.UserId != userId)
            throw new EntityNotFoundException("Analysis");

        return analysis;
    }

    public async Task<IEnumerable<AnalysisDto?>> GetByUserIdAsync(int userId)
    {
        var analyses = await _analysisRepository.GetByUserIdAsync(userId);
        if (analyses == null || !analyses.Any()) return [];

        var analysisDTOs = analyses.Select(analysisEntity =>
        {
            if (analysisEntity == null) return null;

            var dto = Mapper.Map<AnalysisDto>(analysisEntity);

            // Calcula DocumentsCount diretamente da entidade original
            dto.DocumentsCount = analysisEntity.Documents?.Count ?? 0;
            dto.UserName = analysisEntity.User?.FullName ?? "Usuário Desconhecido"; // Exemplo          

            return dto;

        }).Where(dto => dto != null);

        return analysisDTOs;
    }

    public async Task<AnalysisDto> CreateAsync(CreateAnalysisDto createDto, int userId)
    {
        Analysis analysis = Mapper.Map<Analysis>(createDto);
        analysis.UserId = userId; 
        analysis.Status = AnalysisStatus.Draft;

        var createdAnalysis = await _analysisRepository.CreateAsync(analysis);

        _ = await _analysisRepository.GetByIdWithDetailsAsync(createdAnalysis.Id);

        return Mapper.Map<AnalysisDto>(analysis);
    }    

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        if (!await _analysisRepository.BelongsToUserAsync(id, userId))
            throw new AnalysisDontBelongToUserException();

        await _analysisRepository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> PostAnalysisQuestion(int analysisId, StartAnalysisDto startAnalysisDto, int userId)
    {
        if (!await _analysisRepository.BelongsToUserAsync(analysisId, userId))
            throw new AnalysisDontBelongToUserException();

        Analysis? analysis = await _analysisRepository.GetByIdWithDetailsAsync(analysisId);
        if (analysis == null || analysis.UserId != userId)
            throw new EntityNotFoundException("Analysis");

        analysis.Question = startAnalysisDto.Question;
         await _analysisRepository.UpdateAsync(analysis);

        return true;
    }

    public async Task<AnalysisExecutionResult> GetFullStateByIdAsync(int analysisId, int userId)
    {
        if (!await _analysisRepository.BelongsToUserAsync(analysisId, userId))
            throw new AnalysisDontBelongToUserException();

        Analysis? analysis = await _analysisRepository.GetByIdAsync(analysisId);
        if(analysis == null || analysis.UserId != userId) 
            throw new EntityNotFoundException("Analysis");

        AnalysisExecutionResult detailsAnalysis = new AnalysisExecutionResult();

        if (analysis.Stages.Count != 0)
        {
            detailsAnalysis.PreAnalysisResult = (PreAnalysisStage)analysis.Stages.First();

            if(analysis.Stages.Count == 2)
            {
                detailsAnalysis.ExplorationOfMaterialStage = (ExplorationOfMaterialStage)analysis.Stages.Last();
            }
        }
        if (analysis.Question != null)
            detailsAnalysis.AnalysisQuestion = analysis.Question;


        detailsAnalysis.AnalysisTitle = analysis.Title;
        detailsAnalysis.IsSuccess = true;
        detailsAnalysis.ReferenceDocuments = analysis.Documents.ToList().FindAll(x => x.Purpose == DocumentPurpose.Reference);
        detailsAnalysis.AnalysisDocuments = analysis.Documents.ToList().FindAll(x => x.Purpose == DocumentPurpose.Analysis);

        return detailsAnalysis;
    }
}