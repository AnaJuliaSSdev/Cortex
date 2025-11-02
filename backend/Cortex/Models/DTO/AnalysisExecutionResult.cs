namespace Cortex.Models.DTO;

public class AnalysisExecutionResult
{
    public IEnumerable<Document> ReferenceDocuments { get; set; }
    public IEnumerable<Document> AnalysisDocuments { get; set; }

    public PreAnalysisStage PreAnalysisResult {  get; set; }

    public ExplorationOfMaterialStage ExplorationOfMaterialStage { get; set; }
    public string AnalysisTitle { get; set; } = String.Empty;
    public string AnalysisQuestion { get; set; } = String.Empty;

    // ao avanço de cada etapa, os objetos dessa etapa vao sendo preenchidos, e ao final o serviço vai ter
    //todo o contexto de execução da análise

    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; }

    public string PromptResult { get; set; }
}
