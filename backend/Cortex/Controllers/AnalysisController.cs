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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnalysisDto>>> GetAnalyses()
    {
        var userId = GetCurrentUserId();
        var analyses = await _analysisService.GetByUserIdAsync(userId);
        return Ok(analyses);
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
