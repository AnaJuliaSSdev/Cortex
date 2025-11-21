using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IFileStorageService
{
    // <summary>
    /// Salva um arquivo no GCS e no disco local.
    /// </summary>
    /// <returns>Um objeto FileStorageResult contendo os dois caminhos.</returns>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, int analysisId);
    Task<(byte[] FileBytes, string ContentType)> GetFileAsync(string gcsUri);

    /// <summary>
    /// Exclui todos os arquivos associados a uma análise, tanto no 
    /// Google Cloud Storage quanto no sistema de arquivos local.
    /// </summary>
    /// <param name="analysisId">O ID da análise a ser limpa.</param>
    Task DeleteAnalysisStorageAsync(int analysisId);

    /// <summary>
    /// Exclui um único arquivo do GCS e do sistema de arquivos local.
    /// </summary>
    /// <param name="fullLocalPath">O caminho *completo* no disco local.</param>
    /// <param name="gcsUri">O URI GCS (gs://...) do objeto.</param>
    Task DeleteSingleFileAsync(string fullLocalPath, string gcsUri);
}
