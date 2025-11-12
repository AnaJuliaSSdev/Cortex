using Cortex.Models.DTO;
using Cortex.Models.Enums;

namespace Cortex.Services.Interfaces;

public interface IExportService
{
    /// <summary>
    /// Tipo de exportação suportado (PDF, LaTeX, Word, Excel, etc.)
    /// </summary>
    ExportType SupportedType { get; }

    /// <summary>
    /// Exporta os dados da análise para o formato específico
    /// </summary>
    /// <param name="request">Dados da requisição de exportação</param>
    /// <returns>Resultado contendo o arquivo exportado</returns>
    Task<ExportResult> ExportAsync(ExportRequest request);

    /// <summary>
    /// Valida se os dados da análise estão completos para exportação
    /// </summary>
    Task<ValidationResult> ValidateDataAsync(int analysisId);
}
