using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisController(IAnalysisService analysisService, IAnalysisOrchestrator analysisOrchestrator, IAnalysisRepository analysisRepository) : ControllerBase
{
    private readonly IAnalysisService _analysisService = analysisService;
    private readonly IAnalysisOrchestrator _analysisOrchestrator = analysisOrchestrator;
    private readonly IAnalysisRepository _analysisRepository = analysisRepository;


    [HttpDelete("documents/{documentId}")]
    public async Task<ActionResult> DeleteDocument(int documentId)
    {
        var userId = GetCurrentUserId();
        await _analysisService.DeleteDocumentAsync(documentId, userId);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDto<AnalysisDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResultDto<AnalysisDto>>> GetAnalyses(
        [FromQuery] PaginationQueryDto paginationParams)
    {
        int userId = GetCurrentUserId();

        var paginatedResult = await _analysisService.GetByUserIdPaginatedAsync(userId, paginationParams);

        return Ok(paginatedResult);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AnalysisDto>> GetAnalysis(int id)
    {
        var userId = GetCurrentUserId();
        var analysis = await _analysisService.GetByIdAsync(id, userId);
        return Ok(analysis);
    }

    [HttpPost]
    public async Task<ActionResult<AnalysisDto>> CreateAnalysis(CreateAnalysisDto createDto)
    {
        var userId = GetCurrentUserId();
        var analysis = await _analysisService.CreateAsync(createDto, userId);
        return CreatedAtAction(nameof(GetAnalysis), new { id = analysis.Id }, analysis);
    }


    [HttpPost("{analysisId}/question")]
    public async Task<ActionResult<AnalysisDto>> PostAnalysisQuestion(int analysisId, StartAnalysisDto startAnalysisDto)
    {
        var userId = GetCurrentUserId();
        var analysisUpdated = await _analysisService.PostAnalysisQuestion(analysisId, startAnalysisDto, userId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAnalysis(int id)
    {
        var userId = GetCurrentUserId();
        await _analysisService.DeleteAsync(id, userId);
        return NoContent();
    }


    [HttpPost("{id}")]
    public async Task<ActionResult> StartAnalysis(int id)
    {
        var userId = GetCurrentUserId();
        AnalysisExecutionResult response = await _analysisOrchestrator.StartAnalysisAsync(id, userId);
        return Ok(response);
    }

    [HttpPost("reverseLastStage/{id}")]
    public async Task<ActionResult> ReverseLastStage(int id)
    {
        var analise = await _analysisRepository.RevertLastStageAsync(id);
        if(analise != null)
        {
            return NoContent();
        }
        return BadRequest();
    }

    [HttpPost("continue/{id}")]
    public async Task<ActionResult> CotinueAnalysis(int id)
    {
        var analysis = await _analysisRepository.GetByIdAsync(id);
        AnalysisExecutionResult response = await _analysisOrchestrator.ContinueAnalysisAsync(analysis);
        Console.WriteLine(response);
        return Ok(response);
    }

    /// <summary>
    /// Busca o estado completo e atual de uma análise pelo seu ID.
    /// Apenas o dono da análise pode acessá-la.
    /// </summary>
    /// <param name="id">O ID da análise.</param>
    /// <returns>O DTO AnalysisExecutionResult com as etapas preenchidas.</returns>
    [HttpGet("state/{id}")]
    public async Task<ActionResult<AnalysisExecutionResult>> GetAnalysisState(int id)
    {
        var userId = GetCurrentUserId(); 
        var analysisResult = await _analysisService.GetFullStateByIdAsync(id, userId);
        return Ok(analysisResult);
    }

    [HttpGet("{analysisId}/pre-analysis/indexes")]
    public async Task<IActionResult> GetPreAnalysisIndexes(int analysisId)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);

        if (analysis == null)
        {
            return NotFound($"Analysis with ID {analysisId} not found.");
        }

        var preAnalysisStage = analysis.Stages
                                    .OfType<PreAnalysisStage>()
                                    .OrderByDescending(s => s.CreatedAt) 
                                    .FirstOrDefault();

        if (preAnalysisStage == null)
        {
            return Ok(new List<Models.Index>());
        }

        if (preAnalysisStage.Indexes == null)
        {
            return Ok(new List<Models.Index>());
        }
        return Ok(preAnalysisStage.Indexes);
    }


    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }

}
