using Cortex.Exceptions;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class FileStorageService(ILogger<FileStorageService> logger) : IFileStorageService
{
    private readonly string BasePath = "storage/documents";
    private readonly ILogger<FileStorageService> _logger = logger;
    private const int WINDOWS_CHARACTERS_LIMIT = 250;
    private const int MAX_FILE_LENGTH = 50;

    public async Task<string> SaveFileAsync(IFormFile file, int analysisId, string documentExtension)
    {
        var userDirectory = Path.Combine(this.BasePath, $"analysis-{analysisId}");
        var fullDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), userDirectory);

        Directory.CreateDirectory(fullDirectoryPath);

        var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var uniqueFileName = $"{sanitizedFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[..8]{documentExtension}";

        var fullFilePath = Path.Combine(fullDirectoryPath, uniqueFileName);

        if (fullFilePath.Length > WINDOWS_CHARACTERS_LIMIT)
        {
            uniqueFileName = $"doc_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[..8]{documentExtension}";
            fullFilePath = Path.Combine(fullDirectoryPath, uniqueFileName);
        }

        using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream);
        }

        var relativePath = Path.Combine(userDirectory, uniqueFileName);

        _logger.LogInformation("File saved: {FilePath}", relativePath);

        return relativePath.Replace('\\', '/');
    }

    public async Task<byte[]> GetFileAsync(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), normalizedPath);

        var normalizedFullPath = Path.GetFullPath(fullPath);

        if (!File.Exists(normalizedFullPath))
            throw new EntityNotFoundException($"File: {relativePath}");

        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), this.BasePath));
        if (!normalizedFullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException();

        return await File.ReadAllBytesAsync(fullPath);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        if (sanitized.Length > MAX_FILE_LENGTH)
            sanitized = sanitized[..MAX_FILE_LENGTH];

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "document";

        return sanitized;
    }
}
