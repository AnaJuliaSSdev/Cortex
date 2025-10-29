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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnalysisDto>>> GetAnalyses()
    {
        int userId = GetCurrentUserId();
        IEnumerable<AnalysisDto?> analyses = await _analysisService.GetByUserIdAsync(userId);
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

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }


    // <summary>
    /// Gets the Indexes and related data from the PreAnalysisStage of a specific Analysis.
    /// </summary>
    /// <param name="analysisId">The ID of the Analysis to retrieve results from.</param>
    /// <returns>A list of Indexes or NotFound if the analysis or stage doesn't exist.</returns>
    [HttpGet("{analysisId}/pre-analysis/indexes")] // Route: GET /api/analysis/123/pre-analysis/indexes
    [ProducesResponseType(typeof(IEnumerable<Models.Index>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreAnalysisIndexes(int analysisId)
    {

        // 1. Fetch the Analysis using the repository method that includes nested data
        //    Your GetByIdAsync already loads the necessary nested data for PreAnalysisStage
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);

        if (analysis == null)
        {
            return NotFound($"Analysis with ID {analysisId} not found.");
        }

        // 2. Find the PreAnalysisStage within the loaded Stages
        //    We use OfType<>() to filter the collection to the specific derived type.
        //    OrderByDescending is optional but ensures consistency if multiple exist (shouldn't happen).
        var preAnalysisStage = analysis.Stages
                                    .OfType<PreAnalysisStage>()
                                    .OrderByDescending(s => s.CreatedAt) // Get the latest one if multiple exist (unlikely)
                                    .FirstOrDefault();

        if (preAnalysisStage == null)
        {
            // Return NotFound or an empty list depending on preference
            // return NotFound($"PreAnalysisStage not found for Analysis ID {analysisId}.");
            return Ok(new List<Models.Index>()); // Return empty list if stage not found
        }

        // 3. Return the Indexes from the found stage
        //    The Includes in GetByIdAsync should have already loaded Indexes, Indicators, and References.
        //    Check for null just in case the collection wasn't initialized or loaded.
        if (preAnalysisStage.Indexes == null)
        {
            return Ok(new List<Models.Index>()); // Return empty list
        }

        // Return the collection of Indexes. The serializer (with IgnoreCycles) will handle the response.
        return Ok(preAnalysisStage.Indexes);
    }
}
