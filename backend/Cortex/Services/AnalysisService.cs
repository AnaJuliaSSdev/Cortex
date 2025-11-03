using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using StockApp2._0.Mapper;

namespace Cortex.Services;

public class AnalysisService(IAnalysisRepository analysisRepository, IUnitOfWork unitOfWork, 
    IFileStorageService fileStorageService, IDocumentRepository documentRepository) : IAnalysisService
{
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly IDocumentRepository _documentRepository = documentRepository;

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

    public async Task DeleteAsync(int analysisId, int userId)
    {
        // 1. Verificar Autorização
        if (!await _analysisRepository.BelongsToUserAsync(analysisId, userId))
            throw new AnalysisDontBelongToUserException();

        await _unitOfWork.BeginTransactionAsync();

        await _fileStorageService.DeleteAnalysisStorageAsync(analysisId);

        await _analysisRepository.DeleteAsync(analysisId);

        await _unitOfWork.CommitTransactionAsync();
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

    public async Task<PaginatedResultDto<AnalysisDto>> GetByUserIdPaginatedAsync(int userId, PaginationQueryDto paginationParams)
    {
        var totalCount = await _analysisRepository.GetCountByUserIdAsync(userId);

        if (totalCount == 0)
        {
            return new PaginatedResultDto<AnalysisDto>([], 0, paginationParams.PageNumber, paginationParams.PageSize);
        }

        var analyses = await _analysisRepository.GetByUserIdPaginatedAsync(userId, paginationParams.PageNumber, paginationParams.PageSize);

        var analysisDTOs = analyses.Select(analysisEntity =>
        {
            if (analysisEntity == null) return null;
            var dto = Mapper.Map<AnalysisDto>(analysisEntity); // Mapper lida com Document -> DocumentDto
            dto.DocumentsCount = analysisEntity.Documents?.Count ?? 0;
            dto.UserName = analysisEntity.User?.FullName ?? "Usuário Desconhecido";
            return dto;
        })
        .Where(dto => dto != null)
        .ToList();

        return new PaginatedResultDto<AnalysisDto>(analysisDTOs, totalCount, paginationParams.PageNumber, paginationParams.PageSize);
    }

    public async Task DeleteDocumentAsync(int documentId, int userId)
    {
        //  Buscar o documento e sua análise pai
        var document = await _documentRepository.GetByIdWithAnalysisAsync(documentId) ?? throw new EntityNotFoundException(typeof(Document).ToString());

        // Verificar Autorização
        if (document.Analysis.UserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        // Verificar Status da Análise (Regra de Negócio)
        if (document.Analysis.Status != AnalysisStatus.Draft)
        {
            throw new InvalidOperationException("Documentos só podem ser excluídos de análises que estão no status 'Draft'.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Excluir Arquivos (GCS e Local)
            await _fileStorageService.DeleteSingleFileAsync(document.FilePath, document.GcsFilePath);

            await _documentRepository.DeleteAsync(document);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw; 
        }
    }
}