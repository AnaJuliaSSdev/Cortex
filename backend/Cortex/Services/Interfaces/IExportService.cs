using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IExportService
{
    /// <summary>
    /// Orquestra o processo de exportação de uma análise.
    /// </summary>
    /// <param name="analysisId">ID da análise a ser exportada.</param>
    /// <param name="userId">ID do usuário (para autorização).</param>
    /// <param name="format">O formato de exportação (ex: "pdf", "tex").</param>
    /// <param name="requestDto">DTO contendo a imagem do gráfico em Base64.</param>
    /// <returns>Um DTO de resultado contendo os bytes do arquivo e metadados.</returns>
    Task<ExportResult> GenerateExportAsync(int analysisId, int userId, string format, ExportRequestDto requestDto);
}
