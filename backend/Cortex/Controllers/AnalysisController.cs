using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    private readonly IAnalysisService _analysisService = analysisService;

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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
