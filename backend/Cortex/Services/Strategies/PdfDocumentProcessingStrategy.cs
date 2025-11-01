using Cortex.Models;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
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

        Console.WriteLine($"[iTextSharp] Tamanho do arquivo: {fileBytes.Length} bytes");

        using PdfReader pdfReader = new(fileBytes);

        int numberOfPages = pdfReader.NumberOfPages;
        Console.WriteLine($"[iTextSharp] Número de páginas: {numberOfPages}");

        StringBuilder sb = new();

        for (int i = 1; i <= numberOfPages; i++)
        {
            try
            {
                // Tentar extrair texto com estratégia simples
                string pageText = PdfTextExtractor.GetTextFromPage(pdfReader, i);

                Console.WriteLine($"[iTextSharp] Página {i}: {pageText?.Length ?? 0} caracteres");

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.Append(pageText);
                    sb.Append("\n\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[iTextSharp] Erro ao processar página {i}: {ex.Message}");
            }
        }

        string text = sb.ToString().Replace("\0", "").Trim();

        Console.WriteLine($"[iTextSharp] Texto final extraído: {text.Length} caracteres");

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception("Não foi possível extrair texto do PDF. O arquivo pode ser: 1) PDF escaneado (imagem), 2) PDF protegido, ou 3) Usar codificação não suportada. Tente converter o PDF para texto antes de usar.");
        }

        return new Document
        {
            FileType = DocumentType.Pdf,
            Content = text,
            FileName = file.FileName
        };
    }
}