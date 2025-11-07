using Cortex.Models.DTO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cortex.Services;

/// <summary>
/// Implementação do builder de documentos usando QuestPDF.
/// Cria documentos PDF profissionais seguindo padrões acadêmicos.
/// </summary>
public class PdfDocumentBuilder : IDocumentBuilder
{
    // --- CORREÇÃO ARQUITETURAL ---
    // Em vez de compor o documento no construtor,
    // armazenamos uma lista de ações (comandos) a serem executadas.
    private readonly List<Action<ColumnDescriptor>> _contentActions = new();

    // Armazenamos a configuração da página
    private readonly PageSettings _pageSettings;

    // Construtor
    public PdfDocumentBuilder()
    {
        // Configuração da licença
        QuestPDF.Settings.License = LicenseType.Community;

        // Configurações de página padrão ABNT
        _pageSettings = new PageSettings
        {
            Size = PageSizes.A4,
            MarginTop = 3,
            MarginBottom = 2,
            MarginLeft = 3,
            MarginRight = 2,
            FontFamily = Fonts.Arial, // Use um fallback comum
            FontSize = 12
        };
    }

    // --- MÉTODOS DO BUILDER (MODIFICADOS) ---
    // Agora, eles adicionam uma AÇÃO à lista, em vez de
    // tentar modificar um documento já composto.

    public void AddTitle(string title)
    {
        _contentActions.Add(column =>
        {
            column.Item().PaddingBottom(20).Text(title)
                .FontSize(18)
                .Bold()
                .FontColor(Colors.Blue.Darken3);

            column.Item().PaddingBottom(10).LineHorizontal(2)
                .LineColor(Colors.Blue.Darken3);
        });
    }

    public void AddSection(string sectionTitle)
    {
        _contentActions.Add(column =>
        {
            column.Item().PaddingTop(15).PaddingBottom(8).Text(sectionTitle)
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        });
    }

    public void AddParagraph(string text)
    {
        _contentActions.Add(column =>
        {
            column.Item().PaddingBottom(5).Text(text)
                .FontSize(11)
                .LineHeight(1.5f)
                .Justify();
        });
    }

    public void AddTable(TableData tableData)
    {
        if (!tableData.Rows.Any()) return;

        _contentActions.Add(column =>
        {
            column.Item().PaddingTop(10).PaddingBottom(10).Table(table =>
            {
                // Define colunas
                // --- CORREÇÃO NA DEFINIÇÃO DE COLUNAS ---
                // Você precisa definir todas as colunas de uma vez
                table.ColumnsDefinition(columns =>
                {
                    // Exemplo: 1ª coluna maior, outras iguais
                    // Assumindo que a 1ª é "Categoria" e as outras são dados
                    columns.RelativeColumn(1.5f); // Primeira coluna
                    for (int i = 1; i < tableData.Headers.Count; i++)
                    {
                        columns.RelativeColumn(); // Colunas restantes
                    }
                });

                // Cabeçalho
                table.Header(header =>
                {
                    foreach (var headerText in tableData.Headers)
                    {
                        header.Cell().Element(CellStyle).Text(headerText)
                            .FontSize(10)
                            .Bold()
                            .FontColor(Colors.White);
                    }

                    static IContainer CellStyle(IContainer container)
                    {
                        return container
                            .Background(Colors.Blue.Darken2)
                            .Padding(5)
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten1);
                    }
                });

                // Linhas
                var rowIndex = 0;
                foreach (var row in tableData.Rows)
                {
                    var isEven = rowIndex % 2 == 0;
                    rowIndex++;

                    foreach (var cellText in row)
                    {
                        table.Cell().Element(container => CellStyle(container, isEven))
                            .Text(cellText ?? "-")
                            .FontSize(9)
                            .LineHeight(1.3f);
                    }
                }

                static IContainer CellStyle(IContainer container, bool isEven)
                {
                    var backgroundColor = isEven ? Colors.Grey.Lighten3 : Colors.White;
                    return container
                        .Background(backgroundColor)
                        .Padding(5)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten1);
                }
            });

            // Legenda da tabela
            if (!string.IsNullOrEmpty(tableData.Caption))
            {
                column.Item().PaddingTop(3).Text(tableData.Caption)
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }
        });
    }

    public void AddImage(byte[] imageData, string caption)
    {
        _contentActions.Add(column =>
        {
            column.Item().PaddingTop(15).PaddingBottom(10).Column(imageColumn =>
            {
                imageColumn.Item()
                    .AlignCenter()
                    .MaxWidth(450) // Ajustado para margens ABNT (A4 ~15cm útil)
                    .Image(imageData);

                if (!string.IsNullOrEmpty(caption))
                {
                    imageColumn.Item()
                        .PaddingTop(8)
                        .AlignCenter()
                        .Text(caption)
                        .FontSize(10)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                }
            });
        });
    }

    public void AddPageBreak()
    {
        _contentActions.Add(column =>
        {
            column.Item().PageBreak();
        });
    }

    /// <summary>
    /// Gera o documento PDF executando todas as ações de construção.
    /// </summary>
    public byte[] Build()
    {
        // O Document.Create é chamado AQUI, no final.
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Aplica as configurações de página
                page.Size(_pageSettings.Size);
                page.MarginTop(_pageSettings.MarginTop, Unit.Centimetre);
                page.MarginBottom(_pageSettings.MarginBottom, Unit.Centimetre);
                page.MarginLeft(_pageSettings.MarginLeft, Unit.Centimetre);
                page.MarginRight(_pageSettings.MarginRight, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontSize(_pageSettings.FontSize)
                    .FontFamily(_pageSettings.FontFamily)
                    .FontColor(Colors.Black));

                // Cabeçalho (Header)
                page.Header()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.Span("Sistema Cortex - Análise de Conteúdo")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });

                // Rodapé (Footer)
                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });

                // Conteúdo (Content)
                // Executa todas as ações que foram adicionadas
                page.Content().Column(column =>
                {
                    foreach (var action in _contentActions)
                    {
                        action(column); // Executa a ação (ex: column.Item().Text(...) )
                    }
                });
            });
        });

        // Gera o PDF a partir do documento composto
        return document.GeneratePdf();
    }
}

// Classe auxiliar interna para configurações de página
internal class PageSettings
{
    public PageSize Size { get; set; } = PageSizes.A4;
    public float MarginTop { get; set; } = 2;
    public float MarginBottom { get; set; } = 2;
    public float MarginLeft { get; set; } = 2;
    public float MarginRight { get; set; } = 2;
    public string FontFamily { get; set; } = Fonts.Arial;
    public int FontSize { get; set; } = 12;
}

// Defina a interface IDocumentBuilder se ainda não o fez
// (Este arquivo assume que IDocumentBuilder existe)
// Ex: Cortex/Services/Interfaces/IDocumentBuilder.cs
public interface IDocumentBuilder
{
    void AddTitle(string title);
    void AddSection(string sectionTitle);
    void AddParagraph(string text);
    void AddTable(TableData tableData);
    void AddImage(byte[] imageData, string caption);
    void AddPageBreak();
    byte[] Build();
}