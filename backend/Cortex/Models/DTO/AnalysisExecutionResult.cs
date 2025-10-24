namespace Cortex.Models.DTO;

public class AnalysisExecutionResult
{
    public IEnumerable<Document> ReferenceDocuments { get; set; }
    public IEnumerable<Document> AnalysisDocuments { get; set; }

    public PreAnalysisStage PreAnalysisResult {  get; set; }

    // aqui vão ter os resultados de outras etapas
    // ao avanço de cada etapa, os objetos dessa etapa vao sendo preenchidos, e ao final o serviço vai ter
    //todo o contexto de execução da análise

    public bool IsSuccess { get; set; }

    public string ErrorMessage { get; set; }

    public string PromptResult { get; set; }
}
