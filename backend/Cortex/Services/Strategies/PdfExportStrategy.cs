using Cortex.Models;
using Cortex.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cortex.Services.Strategies;

public class PdfExportStrategy : IExportStrategy
{
    private readonly ILogger<PdfExportStrategy> _logger;

    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    public PdfExportStrategy(ILogger<PdfExportStrategy> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> ExportAsync(Analysis analysis, byte[] chartImageBytes)
    {
        _logger.LogInformation("Iniciando geração de PDF para Análise ID: {AnalysisId}...", analysis.Id);

        // 1. Pré-processar dados para a Tabela Resumo
        // (Isso é complexo, envolve pivotar os dados)
        var pivotData = BuildPivotTableData(analysis);

        // 2. Criar o documento QuestPDF
        var document = new AnalysisReportDocument(analysis, chartImageBytes, pivotData);

        // 3. Gerar o PDF em memória
        // Use GeneratePdf() para síncrono ou GeneratePdfAsync() se o documento fizer I/O
        byte[] pdfBytes = document.GeneratePdf();

        _logger.LogInformation("Geração de PDF concluída. Tamanho: {Size} bytes.", pdfBytes.Length);

        return Task.FromResult(pdfBytes);
    }

    /// <summary>
    /// Constrói a estrutura de dados (matriz) para a tabela de resumo (Categoria vs. Índice).
    /// </summary>
    private PivotTableData BuildPivotTableData(Analysis analysis)
    {
        var explorationStage = analysis.Stages.OfType<ExplorationOfMaterialStage>().FirstOrDefault();
        if (explorationStage == null || explorationStage.Categories == null)
            return new PivotTableData(); // Retorna dados vazios

        // 1. Obter todos os Índices únicos mencionados nesta etapa
        var allIndexes = explorationStage.Categories
            .SelectMany(c => c.RegisterUnits)
            .SelectMany(ru => ru.FoundIndices)
            .DistinctBy(i => i.Id)
            .OrderBy(i => i.Name)
            .ToList();

        // 2. Obter todas as Categorias
        var allCategories = explorationStage.Categories.OrderBy(c => c.Name).ToList();

        // 3. Construir a matriz de contagem
        var rows = new List<PivotTableRow>();
        foreach (var category in allCategories)
        {
            var row = new PivotTableRow { CategoryName = category.Name };

            // Calcula a contagem para cada índice
            foreach (var index in allIndexes)
            {
                int count = category.RegisterUnits
                    .Count(ru => ru.FoundIndices.Any(fi => fi.Id == index.Id));

                row.Counts.Add(count);
            }
            rows.Add(row);
        }

        return new PivotTableData
        {
            IndexHeaders = allIndexes.Select(i => i.Name).ToList(),
            Rows = rows
        };
    }
}

// --- Classes Auxiliares para o QuestPDF ---

// DTOs para a tabela pivotada
public class PivotTableData
{
    public List<string> IndexHeaders { get; set; } = new();
    public List<PivotTableRow> Rows { get; set; } = new();
}
public class PivotTableRow
{
    public string CategoryName { get; set; }
    public List<int> Counts { get; set; } = new();
}

/// <summary>
/// Define a estrutura do documento PDF usando QuestPDF.
/// </summary>
public class AnalysisReportDocument : IDocument
{
    private readonly Analysis _analysis;
    private readonly byte[] _chartImageBytes;
    private readonly PivotTableData _pivotData;

    public AnalysisReportDocument(Analysis analysis, byte[] chartImageBytes, PivotTableData pivotData)
    {
        _analysis = analysis;
        _chartImageBytes = chartImageBytes;
        _pivotData = pivotData;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        // Tenta simular margens ABNT (3cm sup/esq, 2cm inf/dir)
        // 1cm = 28.3464567 points
        container.Page(page =>
        {
            page.MarginTop(3, Unit.Centimetre);
            page.MarginBottom(2, Unit.Centimetre);
            page.MarginLeft(3, Unit.Centimetre);
            page.MarginRight(2, Unit.Centimetre);

            // Fonte Padrão (QuestPDF usa Inter, que é similar a Arial/Helvetica)
            page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial")); // Use um fallback comum

            // Cabeçalho (Título da Análise)
            page.Header().Element(ComposeHeader);

            // Conteúdo (Gráfico, Tabela, Detalhamento)
            page.Content().Element(ComposeContent);

            // Rodapé (Número da Página)
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").CurrentPageNumber().FontFamily("Arial");
            });
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text($"Relatório da Análise: {_analysis.Title}")
                .SemiBold().FontSize(16);

            col.Item().Text($"Pergunta Central: {_analysis.Question ?? "N/A"}")
                .Italic().FontSize(10);

            col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#CCC");
        });
    }

    void ComposeContent(IContainer container)
    {
        // Pega os dados das etapas (já carregados pelo Repository)
        var preAnalysis = _analysis.Stages.OfType<PreAnalysisStage>().FirstOrDefault();
        var exploration = _analysis.Stages.OfType<ExplorationOfMaterialStage>().FirstOrDefault();

        container.Column(col =>
        {
            // 1. Gráfico
            col.Item().PaddingBottom(10).Section(section =>
            {
                section.Item().Text("Gráfico de Resultados da Exploração").Bold().FontSize(14);
                section.Item().Image(_chartImageBytes).FitWidth();
            });

            // 2. Tabela Resumo (Pivot)
            col.Item().PaddingBottom(10).Section(section =>
            {
                section.Item().Text("Tabela de Resumo (Frequência de Índices por Categoria)").Bold().FontSize(14);
                section.Item().Table(table =>
                {
                    // Cabeçalho da Tabela
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Coluna da Categoria
                        foreach (var _ in _pivotData.IndexHeaders)
                            columns.RelativeColumn(1); // Colunas dos Índices
                    });

                    // Linha de Cabeçalho
                    table.Header(header =>
                    {
                        header.Cell().Border(1).Background("#EEE").Padding(5).Text("Categoria");
                        foreach (var indexName in _pivotData.IndexHeaders)
                            header.Cell().Border(1).Background("#EEE").Padding(5).Text(indexName);
                    });

                    // Linhas de Dados
                    foreach (var row in _pivotData.Rows)
                    {
                        table.Cell().Border(1).Padding(5).Text(row.CategoryName);
                        foreach (var count in row.Counts)
                            table.Cell().Border(1).Padding(5).AlignCenter().Text(count > 0 ? count.ToString() : "-");
                    }
                });
            });

            // Quebra de Página antes do detalhamento
            col.Item().PageBreak();

            // 3. Detalhamento (Etapa de Exploração)
            if (exploration != null)
            {
                col.Item().Text("Detalhamento da Exploração do Material").Bold().FontSize(16).PaddingBottom(10);
                foreach (var category in exploration.Categories.OrderBy(c => c.Name))
                {
                    col.Item().PaddingBottom(15).Section(catSection =>
                    {
                        // Bloco da Categoria
                        catSection.Item().Background("#EEE").Padding(5).Text(category.Name).Bold().FontSize(14);
                        catSection.Item().Padding(5).Text(category.Definition).Italic();

                        // Unidades de Registro
                        foreach (var unit in category.RegisterUnits)
                        {
                            catSection.Item().PaddingTop(10).Border(1).Padding(8).Column(unitCol =>
                            {
                                // Texto (Citação)
                                unitCol.Item().Background("#F9F9F9").Padding(5)
                                    .Text(text => text.Span($"“{unit.Text}”").Italic());

                                // Justificativa
                                unitCol.Item().PaddingTop(5).Text(text =>
                                {
                                    text.Span("Justificativa: ").SemiBold();
                                    text.Span(unit.Justification);
                                });

                                // Fonte
                                unitCol.Item().Text(text =>
                                {
                                    text.Span("Fonte: ").SemiBold();
                                    text.Span($"{unit.SourceDocumentUri} (Pág: {unit.Page}, Linha: {unit.Line})");
                                });

                                // Índices Associados
                                unitCol.Item().PaddingTop(5).Text("Índices Associados:").SemiBold();
                                unitCol.Item().PaddingLeft(10).Column(indexCol =>
                                {
                                    foreach (var index in unit.FoundIndices)
                                    {
                                        indexCol.Item().Text(text =>
                                        {
                                            // Assume que o Indicator foi carregado (pelo GetByIdAsync)
                                            string indicatorName = index.Indicator?.Name ?? "N/A";
                                            text.Span($"- {index.Name} ").Bold();
                                            text.Span($"(Indicador: {indicatorName})");
                                        });
                                    }
                                });
                            });
                        }
                    });
                }
            }

            // 4. Detalhamento (Etapa de Pré-Análise - Opcional)
            if (preAnalysis != null && preAnalysis.Indexes.Any())
            {
                col.Item().PageBreak();
                col.Item().Text("Resumo da Pré-Análise (Índices e Indicadores)").Bold().FontSize(16).PaddingBottom(10);

                foreach (var index in preAnalysis.Indexes.OrderBy(i => i.Name))
                {
                    col.Item().PaddingBottom(10).Column(indexCol =>
                    {
                        indexCol.Item().Text(index.Name).Bold().FontSize(14);
                        indexCol.Item().Text(text =>
                        {
                            text.Span("Indicador: ").SemiBold();
                            text.Span(index.Indicator?.Name ?? "N/A");
                        });
                        indexCol.Item().Text(text =>
                        {
                            text.Span("Descrição (Índice): ").SemiBold();
                            text.Span(index.Description ?? "N/A");
                        });

                        // Referências do Índice (da Pré-Análise)
                        indexCol.Item().Text("Referências do Índice:").SemiBold();
                        indexCol.Item().PaddingLeft(10).Column(refCol =>
                        {
                            foreach (var reference in index.References)
                            {
                                refCol.Item().Text($"- {reference.SourceDocumentUri} (Pág: {reference.Page}, Linha: {reference.Line})");
                            }
                        });
                    });
                }
            }
        });
    }
}
