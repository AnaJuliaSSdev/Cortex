using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using System.Text;

namespace Cortex.Services.Interfaces;

public abstract class AStageService(IDocumentRepository documentRepository)
{
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly string basePrompt = """
    Você é um pesquisador profissional que utiliza análise de conteúdo como sua principal metodologia de análise.        
    Você segue a metodologia de análise de conteúdo publicada por Laurence Bardin para fazer suas análises.
    Você fará análise de entrevistas semi estruturadas.
    Siga atentamente as seguintes instruções e seja rigoroso e criterioso no seu processo de análise.
    """;

    public virtual async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
    {
        IEnumerable<Document> documents = await _documentRepository.GetByAnalysisIdAsync(analysis.Id);
        IEnumerable<Document> referenceDocuments = documents.Where(x => x.Purpose == Models.Enums.DocumentPurpose.Reference);
        IEnumerable<Document> analysisDocuments = documents.Where(x => x.Purpose == Models.Enums.DocumentPurpose.Analysis);

        var analysisExecutionResult = new AnalysisExecutionResult()
        {
            AnalysisDocuments = analysisDocuments,
            ReferenceDocuments = referenceDocuments
        };
        return analysisExecutionResult;
    }

    /// <summary>
    /// Método abstrato que as subclasses devem implementar para retornar
    /// o TEMPLATE de prompt específico daquela etapa (com placeholders).
    /// </summary>
    /// <returns>A string do template do prompt da etapa.</returns>
    public abstract string GetStagePromptTemplate();

    /// <summary>
    /// Método abstrato que as subclasses devem implementar para formatar
    /// seu template específico com os dados necessários.
    /// </summary>
    /// <param name="analysis">A análise atual.</param>
    /// <param name="resultBaseClass">O resultado contendo os documentos carregados.</param>
    /// <param name="previousStageData">Dados opcionais da etapa anterior (ex: índices formatados).</param>
    /// <returns>O prompt específico da etapa, já formatado.</returns>
    public abstract string FormatStagePromptAsync(Analysis analysis, AnalysisExecutionResult resultBaseClass, object? previousStageData = null);

    /// <summary>
    /// Cria o prompt final combinando o prompt base com o prompt formatado da etapa atual.
    /// </summary>
    /// <param name="analysis">A análise atual.</param>
    /// <param name="resultBaseClass">O resultado contendo os documentos carregados.</param>
    /// <param name="previousStageData">Dados opcionais da etapa anterior.</param>
    /// <returns>O prompt final completo para enviar ao LLM.</returns>
    public string CreateFinalPrompt(Analysis analysis, AnalysisExecutionResult resultBaseClass, object? previousStageData = null)
    {
        // Chama o método abstrato que a subclasse implementará para formatar seu prompt
        string formattedStagePrompt = FormatStagePromptAsync(analysis, resultBaseClass, previousStageData);

        // Concatena o prompt base com o prompt formatado da etapa
        StringBuilder stringBuilder = new();
        stringBuilder.Append(basePrompt);
        stringBuilder.Append(formattedStagePrompt);
        return stringBuilder.ToString();
    }

    protected string GetDocumentNames(IEnumerable<Document> documents, string fallbackMessage = "Nenhum documento fornecido.")
    {
        if (documents != null && documents.Any())
        {
            return string.Join(",\n", documents.Select(d => d.FileName));
        }
        return fallbackMessage;
    }
}
