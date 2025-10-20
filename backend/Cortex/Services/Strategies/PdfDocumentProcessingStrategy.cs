using Cortex.Models;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

namespace Cortex.Services.Strategies;
public class PdfDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public string DocumentExtension => ".pdf";

    public async Task<Document> ProcessAsync(IFormFile file)
    {
        using MemoryStream ms = new();
        await file.CopyToAsync(ms);
        byte[] fileBytes = ms.ToArray();

        using PdfReader pdfReader = new(new MemoryStream(fileBytes));
        using PdfDocument pdfDoc = new(pdfReader);

        StringBuilder sb = new();

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            sb.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
        }

        string text = sb.ToString().Replace("\0", "");

        return new Document
        {
            FileType = DocumentType.Pdf,
            Content = text,
            FileName = file.FileName
        };
    }
}
