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

    [Obsolete]
    public void AddTable(TableData tableData)
    {
        if (tableData == null || tableData.Rows.Count == 0) return;

        _contentActions.Add(column =>
        {
            column.Item().ShowEntire().PaddingVertical(10).Element(container =>
            {
                container.Column(innerColumn =>
                {
                    // --- Título ---
                    if (!string.IsNullOrEmpty(tableData.Caption))
                    {
                        innerColumn.Item().PaddingBottom(5)
                            .Text(tableData.Caption)
                            .FontSize(11)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                    }

                    // --- Tabela ---
                    innerColumn.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            for (int i = 1; i < tableData.Headers.Count; i++)
                                columns.RelativeColumn();
                        });

                        // Cabeçalho
                        table.Header(header =>
                        {
                            foreach (var headerText in tableData.Headers)
                            {
                                header.Cell().Element(CellStyleHeader)
                                    .Text(headerText)
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(Colors.White);
                            }
                        });

                        // Corpo
                        var rowIndex = 0;
                        foreach (var row in tableData.Rows)
                        {
                            var isEven = rowIndex++ % 2 == 0;
                            foreach (var cellText in row)
                            {
                                table.Cell().Element(c => CellStyleBody(c, isEven))
                                    .Text(cellText ?? "-")
                                    .FontSize(9)
                                    .LineHeight(1.3f)
                                     .WrapAnywhere();
                            }
                        }

                        static IContainer CellStyleHeader(IContainer container) =>
                            container.Background(Colors.Blue.Darken2)
                                     .Padding(5)
                                     .BorderBottom(1)
                                     .BorderColor(Colors.Grey.Lighten1);

                        static IContainer CellStyleBody(IContainer container, bool isEven) =>
                            container.Background(isEven ? Colors.Grey.Lighten3 : Colors.White)
                                     .Padding(5)
                                     .BorderBottom(1)
                                     .BorderColor(Colors.Grey.Lighten1);
                    });

                    // --- Legenda ---
                    innerColumn.Item().PaddingTop(3)
                        .Text($"Tabela: {tableData.Caption}")
                        .FontSize(9)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    public void AddImage(byte[] imageData, string caption)
    {
        _contentActions.Add(column =>
        {
            column.Item().EnsureSpace().PaddingTop(15).PaddingBottom(10).Column(imageColumn =>
            {
                imageColumn.Item()
                    .AlignCenter()
                    .PaddingTop(15)
                    .MaxWidth(450) // Ajustado para margens ABNT (A4 ~15cm útil)
                    .Image(imageData);

                if (!string.IsNullOrEmpty(caption))
                {
                    imageColumn.Item()
                        .PaddingTop(8)
                        .PaddingBottom(10)
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
                    .AlignRight()
                    .PaddingRight(0.5f, Unit.Centimetre) 
                    .Text(x =>
                    {
                        x.CurrentPageNumber();
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