using Cortex.Models;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;

namespace Cortex.Services.Strategies;
public class PdfDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public async Task<Document> ProcessAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        using var pdfReader = new PdfReader(new MemoryStream(fileBytes));
        using var pdfDoc = new PdfDocument(pdfReader);

        var sb = new StringBuilder();

        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            sb.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
        }

        var text = sb.ToString().Replace("\0", "");

        return new Document
        {
            FileType = DocumentType.Pdf,
            Content = text,
            FileName = file.FileName,
            FileData = fileBytes
        };
    }
}
