using Google.Cloud.Storage.V1;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class GcsFileStorageService(ILogger<GcsFileStorageService> logger, StorageClient storageClient,
    IConfiguration configuration) : IFileStorageService
{
    private readonly string BasePath = "storage/documents";
    private readonly ILogger<GcsFileStorageService> _logger = logger;
    private readonly StorageClient _storageClient = storageClient;
    private readonly string _bucketName = configuration["GcsSettings:BucketName"]
            ?? throw new ArgumentNullException("GcsSettings:BucketName not found on appsettings.json");
    private const int MAX_FILE_LENGTH = 50;

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, int analysisId)
    {
        var userDirectory = Path.Combine(this.BasePath, $"analysis-{analysisId}"); // cria o diretório do usuário

        // Extrai nome e extensão
        string extension = Path.GetExtension(fileName);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        var sanitizedFileName = SanitizeFileName(nameWithoutExt);

        // Gera nome único
        var uniqueFileName = $"{sanitizedFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[..8]{extension}";
        var gcsObjectName = $"{userDirectory}/{uniqueFileName}";

        if (gcsObjectName.Length > 1024) // Limite do GCS, cuida se é maior que a janela de caracteres deles
        {
            uniqueFileName = $"doc_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..8]}{extension}";
            gcsObjectName = $"{userDirectory}/{uniqueFileName}";
        }

        // Upload para o GCS usando o Stream fornecido
        if (fileStream.Position > 0 && fileStream.CanSeek)
            fileStream.Position = 0;

        //copia o arquivo pro vertex
        await _storageClient.UploadObjectAsync(_bucketName, gcsObjectName, contentType, fileStream);

        var gcsUri = $"gs://{_bucketName}/{gcsObjectName}";
        _logger.LogInformation("Arquivo salvo no GCS: {GcsUri}", gcsUri);

        return gcsUri;
    }

    public async Task<(byte[] FileBytes, string ContentType)> GetFileAsync(string gcsUri)
    {
        if (string.IsNullOrEmpty(gcsUri) || !gcsUri.StartsWith($"gs://{_bucketName}/"))
        {
            throw new ArgumentException("GCS URI inválido.", nameof(gcsUri));
        }

        string objectName = gcsUri.Substring($"gs://{_bucketName}/".Length);

        using (var memoryStream = new MemoryStream())
        {
            // Baixa o objeto do GCS para a memória
            var obj = await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream);
            memoryStream.Position = 0; // Reseta o stream para o início

            return (memoryStream.ToArray(), obj.ContentType);
        }
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
        var gcsPathBase = Path.Combine(this.BasePath, $"analysis-{analysisId}");
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
        //  Excluir Objeto do GCS
        if (string.IsNullOrEmpty(gcsUri) || !gcsUri.StartsWith("gs://"))
        {
            _logger.LogWarning("URI GCS inválido ou vazio, não é possível excluir: {GcsUri}", gcsUri);
            return; // Ou lançar exceção se GCS for obrigatório
        }

        // Parseia o GCS URI para obter o nome do bucket e do objeto
        string uriPrefix = $"gs://{_bucketName}/";
        string objectName = gcsUri.Substring(uriPrefix.Length);

        await _storageClient.DeleteObjectAsync(_bucketName, objectName);
        _logger.LogInformation("Objeto GCS excluído: {GcsUri}", gcsUri);
    }
}
