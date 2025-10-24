using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IFileStorageService
{
    // <summary>
    /// Salva um arquivo no GCS e no disco local.
    /// </summary>
    /// <returns>Um objeto FileStorageResult contendo os dois caminhos.</returns>
    Task<FileStorageResult> SaveFileAsync(IFormFile file, int analysisId, string documentExtension);
    Task<byte[]> GetFileAsync(string filePath);
}
