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
        Siga atentamente as seguintes instruções e seja rigoroso e criterioso no seu processo de análise.
        """;
    }
}
