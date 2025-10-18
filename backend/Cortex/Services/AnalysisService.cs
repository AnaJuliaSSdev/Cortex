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

    public async Task<AnalysisDto?> GetByIdAsync(int id, int userId)
    {
        var analysis = await _analysisRepository.GetByIdWithDetailsAsync(id);

        if (analysis == null || analysis.UserId != userId)
            throw new EntityNotFoundException("Analysis");

        return Mapper.Map<AnalysisDto>(analysis);
    }

    public async Task<IEnumerable<AnalysisDto>> GetByUserIdAsync(int userId)
    {
        var analyses = await _analysisRepository.GetByUserIdAsync(userId);
        return analyses.Select(x => Mapper.Map<AnalysisDto>(x));
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
}