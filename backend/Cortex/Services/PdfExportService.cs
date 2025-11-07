using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

/// <summary>
/// Serviço de exportação para formato PDF.
/// Usa QuestPDF para geração de documentos PDF profissionais.
/// </summary>
public class PdfExportService : IExportService
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisDataFormatter _dataFormatter;
    private readonly ILogger<PdfExportService> _logger;

    public ExportType SupportedType => ExportType.PDF;

    public PdfExportService(
        IAnalysisRepository analysisRepository,
        IAnalysisDataFormatter dataFormatter,
        ILogger<PdfExportService> logger)
    {
        _analysisRepository = analysisRepository;
        _dataFormatter = dataFormatter;
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsync(ExportRequest request)
    {
        _logger.LogInformation("Iniciando exportação PDF para Análise ID: {AnalysisId}", request.AnalysisId);

        try
        {
            // 1. Validar dados
            var validation = await ValidateDataAsync(request.AnalysisId);
            if (!validation.IsValid)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Dados inválidos: {string.Join(", ", validation.Errors)}"
                };
            }

            // 2. Buscar dados da análise
            var analysis = await _analysisRepository.GetByIdAsync(request.AnalysisId);
            if (analysis == null)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Análise não encontrada"
                };
            }

            // 3. Buscar a etapa de exploração (resultados finais)
            var explorationStage = analysis.Stages
                .OfType<ExplorationOfMaterialStage>()
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            if (explorationStage?.Categories == null || !explorationStage.Categories.Any())
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = "Nenhuma categoria encontrada na análise"
                };
            }

            // 4. Construir o documento PDF
            var pdfBuilder = new PdfDocumentBuilder();
            BuildDocument(pdfBuilder, analysis, explorationStage, request);

            // 5. Gerar o arquivo
            var pdfBytes = pdfBuilder.Build();

            _logger.LogInformation("Exportação PDF concluída com sucesso. Tamanho: {Size} bytes", pdfBytes.Length);

            return new ExportResult
            {
                Success = true,
                FileContent = pdfBytes,
                FileName = $"Analise_{analysis.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                MimeType = "application/pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar análise {AnalysisId} para PDF", request.AnalysisId);
            return new ExportResult
            {
                Success = false,
                ErrorMessage = $"Erro ao gerar PDF: {ex.Message}"
            };
        }
    }

    public async Task<ValidationResult> ValidateDataAsync(int analysisId)
    {
        var result = new ValidationResult { IsValid = true };

        var analysis = await _analysisRepository.GetByIdAsync(analysisId);

        if (analysis == null)
        {
            result.IsValid = false;
            result.Errors.Add("Análise não encontrada");
            return result;
        }

        var explorationStage = analysis.Stages
            .OfType<ExplorationOfMaterialStage>()
            .FirstOrDefault();

        if (explorationStage == null)
        {
            result.IsValid = false;
            result.Errors.Add("Etapa de exploração não encontrada");
        }

        if (explorationStage?.Categories == null || !explorationStage.Categories.Any())
        {
            result.IsValid = false;
            result.Errors.Add("Nenhuma categoria encontrada");
        }

        return result;
    }

    private void BuildDocument(
        PdfDocumentBuilder builder,
        Analysis analysis,
        ExplorationOfMaterialStage explorationStage,
        ExportRequest request)
    {
        var options = request.Options;

        // Título principal
        string title = options.CustomTitle ?? $"Resultados da Análise de Conteúdo - {analysis.Title}";
        builder.AddTitle(title);

        // Metadados da análise
        builder.AddSection("Informações da Análise");
        builder.AddParagraph($"Título: {analysis.Title}");
        builder.AddParagraph($"Data de Criação: {analysis.CreatedAt:dd/MM/yyyy}");
        if (!string.IsNullOrEmpty(analysis.Question))
        {
            builder.AddParagraph($"Pergunta Central: {analysis.Question}");
        }
        builder.AddParagraph($"Total de Categorias: {explorationStage.Categories.Count}");
        builder.AddParagraph($"Total de Unidades de Registro: {explorationStage.Categories.Sum(c => c.Frequency)}");

        // Imagem do gráfico
        if (options.IncludeChartImage && request.ChartImage != null && request.ChartImage.Length > 0)
        {
            builder.AddPageBreak();
            builder.AddSection("Distribuição das Categorias");
            builder.AddImage(request.ChartImage, "Gráfico de frequência das categorias e índices");
        }

        // Tabela resumo
        if (options.IncludeSummaryTable)
        {
            builder.AddPageBreak();
            builder.AddSection("Resumo das Categorias");
            var summaryTable = CreateCategorySummaryTable(explorationStage.Categories.ToList());
            builder.AddTable(summaryTable);
        }

        // Detalhamento das categorias
        if (options.IncludeDetailedData)
        {
            builder.AddPageBreak();
            builder.AddSection("Detalhamento das Categorias");

            foreach (var category in explorationStage.Categories.OrderByDescending(c => c.Frequency))
            {
                BuildCategoryDetail(builder, category, options.IncludeReferences);
            }
        }
    }

    private void BuildCategoryDetail(PdfDocumentBuilder builder, Category category, bool includeReferences)
    {
        builder.AddSection($"{category.Name}");
        builder.AddParagraph($"Definição: {category.Definition}");
        builder.AddParagraph($"Frequência: {category.Frequency} unidade(s) de registro");

        // Agrupar unidades de registro por índice
        var unitsByIndex = category.RegisterUnits
            .SelectMany(ru => ru.FoundIndices, (ru, idx) => new { RegisterUnit = ru, Index = idx })
            .GroupBy(x => x.Index.Id)
            .ToList();

        foreach (var indexGroup in unitsByIndex)
        {
            var firstIndex = indexGroup.First().Index;

            builder.AddParagraph($"\nÍndice: {firstIndex.Name}");
            if (!string.IsNullOrEmpty(firstIndex.Description))
            {
                builder.AddParagraph($"Descrição: {firstIndex.Description}");
            }

            if (firstIndex.Indicator != null)
            {
                builder.AddParagraph($"Indicador: {firstIndex.Indicator.Name}");
            }

            // Referências do índice
            if (includeReferences && firstIndex.References != null && firstIndex.References.Any())
            {
                var refTable = new TableData
                {
                    Caption = $"Referências do índice '{firstIndex.Name}'",
                    Headers = new List<string> { "Documento", "Página", "Linha", "Trecho" },
                    Rows = firstIndex.References.Select(r => new List<string>
                    {
                        ExtractDocumentName(r.SourceDocumentUri),
                        r.Page ?? "-",
                        r.Line ?? "-",
                        TruncateText(r.QuotedContent, 100)
                    }).ToList()
                };
                builder.AddTable(refTable);
            }

            // Unidades de registro deste índice
            var unitsForThisIndex = indexGroup.Select(x => x.RegisterUnit).Distinct().ToList();

            if (unitsForThisIndex.Any())
            {
                var unitsTable = new TableData
                {
                    Caption = $"Unidades de Registro - {firstIndex.Name}",
                    Headers = new List<string> { "Texto", "Documento", "Página", "Linha", "Justificativa" },
                    Rows = unitsForThisIndex.Select(ru => new List<string>
                    {
                        TruncateText(ru.Text, 150),
                        ExtractDocumentName(ru.SourceDocumentUri),
                        ru.Page ?? "-",
                        ru.Line ?? "-",
                        TruncateText(ru.Justification, 100)
                    }).ToList()
                };
                builder.AddTable(unitsTable);
            }
        }

        builder.AddPageBreak();
    }

    private TableData CreateCategorySummaryTable(List<Category> categories)
    {
        var table = new TableData
        {
            Caption = "Resumo: Categorias, Índices e Frequências",
            Headers = new List<string> { "Categoria", "Definição", "Frequência", "Índices Relacionados" },
            Rows = new List<List<string>>()
        };

        foreach (var category in categories.OrderByDescending(c => c.Frequency))
        {
            var uniqueIndices = category.RegisterUnits
                .SelectMany(ru => ru.FoundIndices)
                .DistinctBy(idx => idx.Id)
                .Select(idx => idx.Name)
                .ToList();

            var indicesText = uniqueIndices.Any()
                ? string.Join(", ", uniqueIndices)
                : "Nenhum";

            table.Rows.Add(new List<string>
            {
                category.Name,
                TruncateText(category.Definition, 100),
                category.Frequency.ToString(),
                TruncateText(indicesText, 100)
            });
        }

        return table;
    }

    private static string ExtractDocumentName(string uri)
    {
        if (string.IsNullOrEmpty(uri)) return "-";

        // Extrai o nome do arquivo do URI
        try
        {
            return System.IO.Path.GetFileName(new Uri(uri).LocalPath);
        }
        catch
        {
            return uri;
        }
    }

    private static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "-";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }
}