using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace Cortex.Services.Interfaces;

public abstract class AStageService(IDocumentRepository documentRepository)
{
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly string basePrompt = """
    Você é um pesquisador profissional que utiliza análise de conteúdo como sua principal metodologia de análise.        
    Você segue a metodologia de análise de conteúdo publicada por Laurance Bardin para fazer suas análises.
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

    public abstract string GetPromptStageAsync();

    public string CreateFinalPrompt(AnalysisExecutionResult resultBaseClass, Analysis analysis)
    {
        string documentsNamesAnalysis = string.Join(",\n",
            resultBaseClass.AnalysisDocuments.Select(d => d.FileName));

        string documentsNamesReferences = (resultBaseClass.ReferenceDocuments != null && resultBaseClass.ReferenceDocuments.Any())
        ? string.Join(",\n", resultBaseClass.ReferenceDocuments.Select(d => d.FileName))
        : "Nessa análise não foi necessário documentos de referência. ";

        string promptStage = GetPromptStageAsync();
        promptStage = String.Format(promptStage, analysis.Question, documentsNamesAnalysis, documentsNamesReferences);

        StringBuilder stringBuilder = new();
        stringBuilder.Append(basePrompt);
        stringBuilder.Append(promptStage);
        return stringBuilder.ToString();
    }
}
