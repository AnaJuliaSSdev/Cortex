using Cortex.Exceptions;
using Cortex.Models.DTO;
using Cortex.Services.Interfaces;
using Google.Cloud.Storage.V1;

namespace Cortex.Services;

public class GcsFileStorageService(ILogger<GcsFileStorageService> logger, StorageClient storageClient,
    IConfiguration configuration) : IFileStorageService
{
    private readonly string BasePath = "storage/documents";
    private readonly ILogger<GcsFileStorageService> _logger = logger;
    private readonly StorageClient _storageClient = storageClient;
    private readonly string _bucketName = configuration["GcsSettings:BucketName"]
            ?? throw new ArgumentNullException("GcsSettings:BucketName not found on appsettings.json");
    private const int WINDOWS_CHARACTERS_LIMIT = 250;
    private const int MAX_FILE_LENGTH = 50;

    public async Task<FileStorageResult> SaveFileAsync(IFormFile file, int analysisId, string documentExtension)
    {
        var userDirectory = Path.Combine(this.BasePath, $"analysis-{analysisId}"); // cria o diretório do usuário
        var fullDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), userDirectory); // combina com o diretório atual

        Directory.CreateDirectory(fullDirectoryPath); //cria se n existir

        var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName)); // cuida do nome do arquivo

        //gera um nome único de arquivo
        var uniqueFileName = $"{sanitizedFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[..8]{documentExtension}";

        var fullFilePath = Path.Combine(fullDirectoryPath, uniqueFileName); // cria o caminho completo

        var gcsObjectName = $"{userDirectory}/{uniqueFileName}"; // Ex: "analysis-123/meu_doc_...pdf"

        if (gcsObjectName.Length > 1024) // Limite do GCS, cuida se é maior que a janela de caracteres deles
        {
            uniqueFileName = $"doc_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..8]}{documentExtension}";
            gcsObjectName = $"{userDirectory}/{uniqueFileName}";
        }

        //CUIDAR ORDEM PARA NÃO CRIAR O RESTO SE FALHAR

        if (fullFilePath.Length > WINDOWS_CHARACTERS_LIMIT) // verifica se é maior que a janela de caracteres do google
        {
            uniqueFileName = $"doc_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[..8]{documentExtension}";
            fullFilePath = Path.Combine(fullDirectoryPath, uniqueFileName);
        }

        //copia o arquivo pro diretório
        using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(fileStream);
        }

        //copia o arquivo pro vertex
        using (var stream = file.OpenReadStream())
        {
            await _storageClient.UploadObjectAsync(_bucketName, gcsObjectName, file.ContentType, stream);
        }
        var gcsUri = $"gs://{_bucketName}/{gcsObjectName}";
        _logger.LogInformation("Arquivo salvo no GCS: {GcsUri}", gcsUri);

        var relativePath = Path.Combine(userDirectory, uniqueFileName);

        _logger.LogInformation("File saved: {FilePath}", relativePath);

        FileStorageResult paths = new()
        {
            GcsPath = gcsUri,
            LocalPath = fullFilePath
        };

        return paths;
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

    public async Task DeleteAnalysisStorageAsync(int analysisId)
    {
        //Excluir Pasta Local
        var userDirectoryName = $"analysis-{analysisId}";
        var userDirectoryPath = Path.Combine(this.BasePath, userDirectoryName);
        var fullDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), userDirectoryPath);

        if (Directory.Exists(fullDirectoryPath))
        {
            Directory.Delete(fullDirectoryPath, recursive: true);
            _logger.LogInformation("Diretório local excluído: {DirectoryPath}", fullDirectoryPath);
        }
        else
        {
            _logger.LogWarning("Diretório local não encontrado para exclusão: {DirectoryPath}", fullDirectoryPath);
        }

        var gcsPathBase = Path.Combine(this.BasePath, userDirectoryName);
        var gcsPrefix = $"{gcsPathBase}/";

        _logger.LogInformation("Iniciando exclusão de objetos GCS com prefixo: {Prefix} no bucket {Bucket}", gcsPrefix, _bucketName);
        // Lista todos os objetos com o prefixo
        var objects = _storageClient.ListObjectsAsync(_bucketName, gcsPrefix);
        var deleteTasks = new List<Task>();
        int count = 0;

        await foreach (var obj in objects)
        {
            // Adiciona a tarefa de exclusão à lista
            deleteTasks.Add(_storageClient.DeleteObjectAsync(obj.Bucket, obj.Name));
            count++;
        }

        // Executa todas as exclusões em paralelo
        if (deleteTasks.Any())
        {
            await Task.WhenAll(deleteTasks);
            _logger.LogInformation("Excluídos {Count} objetos do GCS com prefixo: {Prefix}", count, gcsPrefix);
        }
        else
        {
            _logger.LogInformation("Nenhum objeto encontrado no GCS com prefixo: {Prefix}", gcsPrefix);
        }

    }

    public async Task DeleteSingleFileAsync(string fullLocalPath, string gcsUri)
    {
        // Excluir Arquivo Local
        if (File.Exists(fullLocalPath))
        {
            File.Delete(fullLocalPath);
            _logger.LogInformation("Arquivo local excluído: {FilePath}", fullLocalPath);
        }
        else
        {
            _logger.LogWarning("Arquivo local não encontrado para exclusão: {FilePath}", fullLocalPath);
        }

        //  Excluir Objeto do GCS
        if (string.IsNullOrEmpty(gcsUri) || !gcsUri.StartsWith("gs://"))
        {
            _logger.LogWarning("URI GCS inválido ou vazio, não é possível excluir: {GcsUri}", gcsUri);
            return; // Ou lançar exceção se GCS for obrigatório
        }

        // Parseia o GCS URI para obter o nome do bucket e do objeto
        var gcsPath = GcsPath.Parse(gcsUri);

        await _storageClient.DeleteObjectAsync(gcsPath.Bucket, gcsPath.ObjectName);
        _logger.LogInformation("Objeto GCS excluído: {GcsUri}", gcsUri);
    }
}
