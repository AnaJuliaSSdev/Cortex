using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services.ServicosTopicos;

public class RagService : IRagService
{
    private readonly IChunkRepository _chunkRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IChunkRepository chunkRepository,
        IEmbeddingService embeddingService,
        IGeminiService geminiService,
        ILogger<RagService> logger)
    {
        _chunkRepository = chunkRepository;
        _embeddingService = embeddingService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<string> AskQuestionAsync(string question, int documentId)
    {
        _logger.LogInformation("Gerando embedding da pergunta...");
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

        _logger.LogInformation("Buscando chunks relevantes do documento {DocumentId}...", documentId);
        var relevantChunks = await _chunkRepository.SearchSimilarByDocumentIdAsync(
            documentId,
            questionEmbedding,
            limit: 5
        );

        if (!relevantChunks.Any())
        {
            _logger.LogWarning("Nenhum chunk relevante encontrado para a pergunta.");
            return "Desculpe, não encontrei informações relevantes no documento para responder sua pergunta.";
        }

        _logger.LogInformation("Encontrados {Count} chunks relevantes.", relevantChunks.Count);

        var context = string.Join("\n\n---\n\n", relevantChunks.Select(c => c.Content));

        var prompt = $@"Você é um assistente que responde perguntas baseado APENAS no contexto fornecido abaixo.

        CONTEXTO DO DOCUMENTO:
        {context}

        PERGUNTA DO USUÁRIO:
        {question}

        INSTRUÇÕES:
        - Responda APENAS com base no contexto fornecido
        - Se a informação não estiver no contexto, diga claramente que não encontrou
        - Seja claro, objetivo e preciso
        - Cite partes relevantes do contexto quando apropriado

        RESPOSTA:";

        _logger.LogInformation("Enviando prompt para o Gemini...");
        var response = await _geminiService.GenerateTextAsync(prompt);

        if (!response.IsSuccess)
        {
            _logger.LogError("Erro ao gerar resposta: {Error}", response.ErrorMessage);
            return $"Erro ao gerar resposta: {response.ErrorMessage}";
        }

        var answer = response.FullResponse?.Text ?? "Não foi possível gerar uma resposta.";
        _logger.LogInformation("Resposta gerada com sucesso.");

        return answer;
    }
}