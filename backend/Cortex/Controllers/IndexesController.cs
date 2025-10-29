using Cortex.Models.DTO;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexesController(IIndexRepository indexRepository) : ControllerBase
{

    private readonly IIndexRepository _indexRepository = indexRepository;


    [HttpGet]
    public async Task<ActionResult> GetAllIndexes()
    {
        return Ok(_indexRepository.GetAll());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetAnalysis(int id)
    {
        return Ok(await _indexRepository.GetByIdAsync(id));
    }
}
