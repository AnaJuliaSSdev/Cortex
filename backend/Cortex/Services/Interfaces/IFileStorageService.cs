namespace Cortex.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, int analysisId, string documentExtension);
    Task<byte[]> GetFileAsync(string filePath);
}
