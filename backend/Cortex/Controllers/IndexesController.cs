using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Index = Cortex.Models.Index;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IndexesController(IIndexRepository indexRepository, IIndexService indexService) : ControllerBase
{

    private readonly IIndexRepository _indexRepository = indexRepository;
    private readonly IIndexService _indexService = indexService;


    [HttpGet]
    public ActionResult GetAllIndexes()
    {
        return Ok(_indexRepository.GetAll());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetAnalysis(int id)
    {
        return Ok(await _indexRepository.GetByIdAsync(id));
    }

    [HttpPost("createManual")]
    public async Task<ActionResult<Models.Index>> CreateManualIndex([FromBody] CreateManualIndexDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _indexService.CreateManualIndexAsync(userId, dto);

        // Retorna o novo Índice (com o Indicador aninhado)
        // O EF automaticamente popula o 'indicator'
        return CreatedAtAction(nameof(GetAllIndexes), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Index>> UpdateIndex(int id, [FromBody] UpdateIndexDto dto)
    {
        var userId = GetCurrentUserId();
        Index retorno = await _indexService.UpdateIndexAsync(id, userId, dto);
        return Ok(retorno); // Retorna o índice atualizado
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteIndex(int id)
    {
        int userId = GetCurrentUserId();
        await _indexService.DeleteIndexAsync(id, userId);

        return NoContent(); // Sucesso, sem conteúdo
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
