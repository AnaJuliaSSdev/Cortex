using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers;

[ApiController]
[Route("api")]
public class ChunksController(IChunkRepository chunkRepository) : ControllerBase
{
    private readonly IChunkRepository _chunkRepository = chunkRepository;

    [HttpGet("documents/{documentId}/chunks")]
    [ProducesResponseType(typeof(IEnumerable<ChunkDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetChunksByDocumentId(int documentId)
    {
        var chunks = await _chunkRepository.GetByDocumentIdAsync(documentId);

        if (!chunks.Any())
        {
            return NotFound($"Nenhum chunk encontrado para o Documento ID: {documentId}");
        }

        var chunkDtos = chunks.Select(chunk => new ChunkDto
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content,
            TokenCount = chunk.TokenCount,
            EmbeddingPreview = $"{chunk.Embedding.ToArray().Length}-dimensional vector"
        });

        return Ok(chunkDtos);
    }
}
