using Cortex.Models.Enums;

namespace Cortex.Models.DTO;

public class ProcessedDocumentResult
{
    public Document DocumentModel { get; set; } // O objeto para salvar no Banco
    public Stream FileStream { get; set; }      // O arquivo (original ou convertido) para o GCS
    public string ContentType { get; set; }     // ex: "application/pdf"
    public string Extension { get; set; }       // ex: ".pdf"
    public DocumentType DocumentType { get; set; }
}
