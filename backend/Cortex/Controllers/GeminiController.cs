using Microsoft.AspNetCore.Mvc;
using Cortex.Services.Interfaces;
using Cortex.Models;

namespace GeminiService.Api.Controllers;


/// <summary>
/// Tests purposes only
/// </summary>
/// <param name="geminiService"></param>
[ApiController]
[Route("api/[controller]")]
public class GeminiController(IGeminiService geminiService) : ControllerBase
{
    private readonly IGeminiService _geminiService = geminiService;

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateText([FromBody] GeminiRequest request)
    {
        var result = await _geminiService.GenerateTextAsync(
            request.Prompt,
            request.Model,
            request.ApiKey);

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(new { Response = result.Content });
    }
}