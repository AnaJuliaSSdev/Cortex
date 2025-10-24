namespace Cortex.Models.DTO;

public class FileStorageResult
{
    /// <summary>
    /// O caminho relativo no disco local do servidor.
    /// Ex: "storage/documents/analysis-123/doc_abc.pdf"
    /// </summary>
    public string LocalPath { get; set; }

    /// <summary>
    /// O URI completo do Google Cloud Storage.
    /// Ex: "gs://cortex-analysis/analysis-123/doc_abc.pdf"
    /// </summary>
    public string GcsPath { get; set; }
}
