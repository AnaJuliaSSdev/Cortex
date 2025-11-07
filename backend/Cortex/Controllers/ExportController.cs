using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportServiceFactory _exportFactory;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportServiceFactory exportFactory,
        ILogger<ExportController> logger)
    {
        _exportFactory = exportFactory;
        _logger = logger;
    }

    /// <summary>
    /// Exporta uma análise para PDF
    /// </summary>
    [HttpPost("pdf/{analysisId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportToPdf(
        int analysisId,
        [FromBody] ExportRequestDto requestDto)
    {
        _logger.LogInformation("Requisição de exportação PDF para análise {AnalysisId}", analysisId);

        try
        {
            var userId = GetCurrentUserId();

            // Criar serviço de exportação PDF
            var exportService = _exportFactory.CreateExportService(ExportType.PDF);

            // Converter DTO para request
            var exportRequest = new ExportRequest
            {
                AnalysisId = analysisId,
                ChartImage = !string.IsNullOrWhiteSpace(requestDto.ChartImageBase64)
                ? Convert.FromBase64String(
                    requestDto.ChartImageBase64.Contains(",")
                        ? requestDto.ChartImageBase64.Split(',')[1] // remove o "data:image/png;base64,"
                        : requestDto.ChartImageBase64
                  )
                : null,
                 Options = requestDto.Options ?? new ExportOptions()
            };

            // Validar dados
            var validation = await exportService.ValidateDataAsync(analysisId);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    message = "Dados inválidos para exportação",
                    errors = validation.Errors
                });
            }

            // Executar exportação
            var result = await exportService.ExportAsync(exportRequest);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Retornar arquivo
            return File(
                result.FileContent!,
                result.MimeType!,
                result.FileName!
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar análise {AnalysisId} para PDF", analysisId);
            return StatusCode(500, new { message = "Erro interno ao gerar exportação" });
        }
    }

    /// <summary>
    /// Lista os tipos de exportação disponíveis
    /// </summary>
    [HttpGet("available-types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableExportTypes()
    {
        var types = _exportFactory.GetSupportedTypes()
            .Select(t => t.ToString())
            .ToList();

        return Ok(types);
    }

    /// <summary>
    /// Valida se uma análise pode ser exportada
    /// </summary>
    [HttpGet("validate/{analysisId}/{exportType}")]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateExport(int analysisId, string exportType)
    {
        try
        {
            if (!Enum.TryParse<ExportType>(exportType, true, out var type))
            {
                return BadRequest(new { message = "Tipo de exportação inválido" });
            }

            var service = _exportFactory.CreateExportService(type);
            var result = await service.ValidateDataAsync(analysisId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar exportação");
            return StatusCode(500, new { message = "Erro ao validar dados" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}

// DTO para requisição
public class ExportRequestDto
{
    /// <summary>
    /// Imagem do gráfico em Base64
    /// </summary>
    public string? ChartImageBase64 { get; set; }

    /// <summary>
    /// Opções de exportação
    /// </summary>
    public ExportOptions? Options { get; set; }
}