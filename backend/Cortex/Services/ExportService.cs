using Cortex.Exceptions;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Cortex.Services;

public class ExportService : IExportService
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly ExportStrategyFactory _exportFactory;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IAnalysisRepository analysisRepository,
        ExportStrategyFactory exportFactory,
        ILogger<ExportService> logger)
    {
        _analysisRepository = analysisRepository;
        _exportFactory = exportFactory;
        _logger = logger;
    }

    public async Task<ExportResult> GenerateExportAsync(int analysisId, int userId, string format, ExportRequestDto requestDto)
    {
        // 1. Autorização
        if (!await _analysisRepository.BelongsToUserAsync(analysisId, userId))
        {
            _logger.LogWarning("Falha de autorização: Usuário {UserId} tentou exportar Análise {AnalysisId}", userId, analysisId);
            throw new UnauthorizedAccessException("O usuário não tem permissão para exportar esta análise.");
        }

        // 2. Obter Dados Completos
        _logger.LogInformation("Buscando dados completos da Análise ID: {AnalysisId} para exportação...", analysisId);
        // Usamos GetByIdAsync que já carrega TUDO (PreAnalysisStage, ExplorationStage, etc.)
        var analysis = await _analysisRepository.GetByIdAsync(analysisId);
        if (analysis == null)
            throw new EntityNotFoundException("Análise não encontrada.");

        // 3. Processar Imagem Base64
        byte[] chartImageBytes = ConvertBase64ToBytes(requestDto.ChartImageBase64);

        // 4. Obter Estratégia
        _logger.LogDebug("Obtendo estratégia de exportação para o formato: {Format}", format);
        IExportStrategy strategy = _exportFactory.GetStrategy(format);

        // 5. Executar Estratégia
        _logger.LogDebug("Executando estratégia {StrategyName}...", strategy.GetType().Name);
        byte[] fileBytes = await strategy.ExportAsync(analysis, chartImageBytes);

        // 6. Preparar Resultado
        string fileName = $"Analise_{analysisId}_{analysis.Title?.Replace(" ", "_")?.ToLower() ?? "relatorio"}.{strategy.FileExtension}";

        return new ExportResult
        {
            FileBytes = fileBytes,
            ContentType = strategy.ContentType,
            FileName = fileName
        };
    }

    /// <summary>
    /// Converte uma string de dados Base64 (com ou sem prefixo "data:image/png;base64,")
    /// em um array de bytes.
    /// </summary>
    private byte[] ConvertBase64ToBytes(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            throw new ArgumentNullException(nameof(base64String), "A imagem do gráfico não pode ser nula.");

        // Remove o prefixo (ex: "data:image/png;base64,") se ele existir
        var match = Regex.Match(base64String, @"^data:image\/(?<type>.+?);base64,(?<data>.+)$");
        string cleanBase64 = match.Success ? match.Groups["data"].Value : base64String;

        try
        {
            return Convert.FromBase64String(cleanBase64);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Falha ao converter a string Base64 da imagem do gráfico.");
            throw new ArgumentException("O formato da string Base64 da imagem do gráfico é inválido.", nameof(base64String), ex);
        }
    }
}
