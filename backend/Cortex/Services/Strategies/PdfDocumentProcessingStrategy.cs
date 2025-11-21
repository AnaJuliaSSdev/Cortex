using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;

namespace Cortex.Services.Strategies;

public class PdfDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public string DocumentExtension => ".pdf";

    public async Task<ProcessedDocumentResult> ProcessAsync(IFormFile file)
    {
        MemoryStream ms = new();
        await file.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();

        ms.Position = 0;

        // Extrair texto
        using PdfReader pdfReader = new(fileBytes); // Usa bytes para não travar stream
        int numberOfPages = pdfReader.NumberOfPages;
        StringBuilder sb = new();

        for (int i = 1; i <= numberOfPages; i++)
        {
            string pageText = PdfTextExtractor.GetTextFromPage(pdfReader, i);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                sb.Append(pageText);
                sb.Append("\n\n");
            }
        }

        string text = sb.ToString().Replace("\0", "").Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "";
        }

        // Retornar resultado
        // O stream de upload deve estar no início
        ms.Position = 0;

        var documentModel = new Models.Document
        {
            FileType = DocumentType.Pdf,
            Content = text,
            FileName = file.FileName
        };

        return new ProcessedDocumentResult
        {
            DocumentModel = documentModel,
            FileStream = ms, // Retornamos o PDF original
            ContentType = "application/pdf",
            Extension = DocumentExtension, 
            DocumentType = DocumentType.Pdf
        };
    }
}