using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Cortex.Models.DTO;
using Cortex.Services;
using Cortex.Models;
using Cortex.Models.Enums;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController(DocumentService documentService) : ControllerBase
{
    private readonly DocumentService _documentService = documentService;

    [HttpPost("upload/{analysisId}")]
    public async Task<IActionResult> Upload(int analysisId, [FromForm] CreateDocumentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var document = await _documentService.UploadAsync(dto, analysisId);
        return Ok(document);
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
        return File(document!.FileData, GetContentType(document.FileType), document.FileName);
    }

    private static string GetContentType(DocumentType type) => type switch
    {
        DocumentType.Pdf => "application/pdf",
        DocumentType.Text => "text/plain",
        DocumentType.Doc => "application/msword",
        DocumentType.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream",
    };
}
