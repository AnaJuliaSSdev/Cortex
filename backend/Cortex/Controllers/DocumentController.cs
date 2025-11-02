using Cortex.Exceptions;
using Cortex.Helpers;
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
                
        byte[] fileBytes = await _fileStorageService.GetFileAsync(document!.FilePath);
        return File(fileBytes, document.FileType.ToMimeType(), document.FileName);
    }
}
