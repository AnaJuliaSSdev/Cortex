namespace Cortex.Models.DTO;

public class AnalysisExecutionResult
{
    public IEnumerable<Document> ReferenceDocuments { get; set; }
    public IEnumerable<Document> AnalysisDocuments { get; set; }
}
