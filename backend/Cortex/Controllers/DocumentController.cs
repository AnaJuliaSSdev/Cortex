using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockApp2._0.Mapper;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController(IDocumentService documentService, IFileStorageService fileStorageService) : ControllerBase
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    [HttpPost("upload/{analysisId}")]
    [RequestSizeLimit(104857600)]
    public async Task<IActionResult> Upload(int analysisId, [FromForm] CreateDocumentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        Document document = await _documentService.UploadAsync(dto, analysisId);

        return Ok(Mapper.Map<ViewDocumentDTO>(document));
    }

    /// <summary>
    /// This endpoint is for tests purposes only
    /// </summary>
    /// <param name="id">File id</param>
    /// <returns>The file with this id</returns>
    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        Document? document = await _documentService.GetByIdAsync(id);
        if (document == null)
            throw new EntityNotFoundException(nameof(document));

        var (fileBytes, contentType) = await _fileStorageService.GetFileAsync(document.GcsFilePath);

        return File(fileBytes, contentType, document.FileName);
    }

    /// <summary>
    /// Retorna apenas o CONTEÚDO DE TEXTO do documento salvo no banco.
    /// Usado para visualização rápida de arquivos originais TXT (mesmo que convertidos para PDF).
    /// </summary>
    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(int id)
    {
        Document? document = await _documentService.GetByIdAsync(id);
        if (document == null)
            throw new EntityNotFoundException(nameof(document));

        // Retorna o conteúdo como texto puro
        return Content(document.Content ?? "", "text/plain");
    }
}
