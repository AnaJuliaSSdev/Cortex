using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;

namespace Cortex.Services.Interfaces;

public abstract class AStageService(IDocumentRepository documentRepository)
{
    private readonly IDocumentRepository _documentRepository = documentRepository;

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

    public virtual string GetPromptStageAsync()
    {
        return """
        Você é um pesquisador profissional que utiliza análise de conteúdo como sua principal metodologia de análise.        
        Você segue a metodologia de análise de conteúdo publicada por Laurance Bardin para fazer suas análises.
        Você fará análise de entrevistas semi estruturadas.
        Siga atentamente as seguintes instruções e seja rigoroso e criterioso no seu processo de análise.
        """;
    }

    public string CreateFinalPrompt(AnalysisExecutionResult resultBaseClass, Analysis analysis)
    {
        string promptStage = GetPromptStageAsync();
        string documentsNamesAnalysis = string.Join(",\n",
            resultBaseClass.AnalysisDocuments.Select(d => d.FileName));

        string documentsNamesReferences = string.Join(",\n",
            resultBaseClass.ReferenceDocuments.Select(d => d.FileName));

        return String.Format(promptStage, analysis.Question, documentsNamesAnalysis, documentsNamesReferences);
    }
}
