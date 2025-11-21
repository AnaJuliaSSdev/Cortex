using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cortex.Services.Strategies;

public class TxtDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public string DocumentExtension => ".txt";

    public async Task<ProcessedDocumentResult> ProcessAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        string content = await reader.ReadToEndAsync();

        //CONVERSÃO PARA PDF PARA SER APLICADO O RAG PROPRIAMENTE
        QuestPDF.Settings.License = LicenseType.Community;
        var pdfStream = new MemoryStream();

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842); //A4 equivalente
                page.Margin(2, Unit.Centimetre);
                page.PageColor("FFFFFF");

                page.DefaultTextStyle(x => x
                  .FontSize(11)
                  .FontFamily("Arial") 
              );

                page.Header()
                    .Text($"Documento Convertido: {file.FileName}")
                    .SemiBold()
                    .FontSize(10)
                   .FontColor("777777"); //hexadecimal 

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Text(content); // O texto extraído vira o corpo do PDF

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(pdfStream);

        // Reseta a posição do stream para que possa ser lido no upload
        pdfStream.Position = 0;

        //Criar o Modelo do Documento
        //No banco, ele será salvo como PDF e com nome .pdf
        var documentModel = new Models.Document
        {
            FileType = DocumentType.Text, // mantém a rastreabilidade de que era um txt
            FileName = Path.ChangeExtension(file.FileName, ".pdf"), // aqui o nome é alterado pois é usado no prompt, e pra não confundir é melhor ficar como pdf, pois o vertex vai ler como pdf
            Content = content // Mantemos o texto original extraído para buscas
        };

        // Retornar o resultado composto
        return new ProcessedDocumentResult
        {
            DocumentModel = documentModel,
            FileStream = pdfStream, // Enviamos o PDF gerado, não o TXT original
            ContentType = "application/pdf", // pdf pois foi convertido
            Extension = DocumentExtension, //extensão original para manter a rastreabilidade de que era um txt
            DocumentType = DocumentType.Text
        };
    }
}