using Cortex.Models.Enums;

namespace Cortex.Helpers;

public static class DocumentTypeExtensions
{
    /// <summary>
    /// Converte um valor de DocumentType para sua string de MIME type correspondente.
    /// </summary>
    /// <param name="docType">O tipo de documento.</param>
    /// <returns>A string do MIME type.</returns>
    /// <exception cref="NotSupportedException">Lançada se o tipo de documento não tiver um MIME type mapeado.</exception>
    public static string ToMimeType(this DocumentType docType)
    {
        return docType switch
        {
            DocumentType.Pdf => "application/pdf",
            DocumentType.Text => "text/plain",
            DocumentType.Doc => "application/msword",
            DocumentType.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => throw new NotSupportedException($"The document type '{docType}' does not have a configured MIME type.")
        };
    }
}
