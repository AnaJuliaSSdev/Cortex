using Cortex.Models;
using Cortex.Services.Interfaces;
using GeminiService.Api.Extensions;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeminiService.Api.Services.Implementations;

public class GeminiService(ILogger<GeminiService> logger, IOptions<GeminiConfiguration> settings) : IGeminiService
{
    private readonly GeminiConfiguration _settings = settings.Value;
    private readonly ILogger<GeminiService> _logger = logger;
    private static readonly HttpClient _httpClient = new (); 

    //ONLY TEXT
    public async Task<GeminiResponse> GenerateTextAsync(string prompt, Cortex.Models.Enums.GeminiModel? modelOverride = null, string? apiKeyOverride = null)
    {
        var apiKey = apiKeyOverride ?? _settings.ApiKey;
        var modelName = modelOverride.HasValue
            ? modelOverride.Value.ToApiString()
            : _settings.DefaultModel;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("API Key do Gemini não foi configurada.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = "API Key do Gemini não está configurada." };
        }

        try
        {
            var model = new GenerativeModel(apiKey, model: modelName);

            var response = await model.GenerateContentAsync(prompt);

            return new GeminiResponse { IsSuccess = true, FullResponse = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao chamar a API do Gemini.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = $"Erro ao se comunicar com a API: {ex.Message}" };
        }
    }

    //SEND INLINE DOCUMENTS USING 'PARTS'
    public async Task<GeminiResponse> GenerateContentAsync(string prompt, IEnumerable<Part> fileParts, Cortex.Models.Enums.GeminiModel? modelOverride = null, string? apiKeyOverride = null)
    {
        string apiKey = apiKeyOverride ?? _settings.ApiKey;
        string modelName = modelOverride.HasValue
            ? modelOverride.Value.ToApiString()
            : _settings.DefaultModel;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("API Key do Gemini não foi configurada.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = "API Key do Gemini não está configurada." };
        }

        try
        {
            GenerationConfig generationConfig = new()
            {
                ResponseMimeType = MediaTypeNames.Application.Json,
            };

            GenerativeModel model = new(apiKey, model: modelName, config: generationConfig);

            List<Part> content = [new() { Text = prompt }, .. fileParts];

            GenerateContentRequest request = new() { Contents = [new Content { Parts = content }] };

            GenerateContentResponse response = await model.GenerateContentAsync(request);

             return new GeminiResponse { IsSuccess = true, FullResponse = response, Candidates = response.Candidates};
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao chamar a API do Gemini com múltiplos conteúdos.");
            return new GeminiResponse { IsSuccess = false, ErrorMessage = $"Erro ao se comunicar com a API: {ex.Message}" };
        }
    }

    //só vai precisar desse se conseguir usar o candidates, mas sem o candidates, só enviando os documentos normal, 
    // não precisa fazer isso, e aí pode só pedir pra ele retornar as paginas marcadas, linhas etc
    //SEND DOCUMENTS USING API FILE FROM GEMINI
    public async Task<FileDetails?> UploadFileWithHttpAsync(byte[] fileBytes, string mimeType, string fileName)
    {
         var apiKey =  _settings.ApiKey;
        // Endpoint da File API
        var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";

        try
        {
            using var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("x-goog-file-name", fileName);
            request.Content = content;

            _logger.LogInformation("Enviando arquivo {FileName} ({SizeKB} KB) para a File API...", fileName, fileBytes.Length / 1024);

            var response = await _httpClient.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var uploadResult = JsonSerializer.Deserialize<FileUploadResponse>(jsonResponse);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro no upload do arquivo para a File API. Status: {StatusCode}, Resposta: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }
            _logger.LogInformation("Upload do arquivo {FileName} bem-sucedido. URI: {FileUri}", fileName, uploadResult?.File?.Uri);

            return uploadResult?.File;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção durante o upload do arquivo {FileName}.", fileName);
            return null;
        }
    }

    public class FileUploadResponse
    {
        [JsonPropertyName("file")]
        public FileDetails File { get; set; }
    }

    public class FileDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; }
    }

    public class ManualGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; }

        [JsonPropertyName("tools")]
        public List<ManualTool> Tools { get; set; }

        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; }
    }

    public class ManualTool
    {
        // A propriedade que faltava na biblioteca!
        [JsonPropertyName("grounding")]
        public object Grounding { get; set; } = new object(); // Um objeto vazio ativa o grounding
    }
}