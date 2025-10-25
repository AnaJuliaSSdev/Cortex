namespace Cortex.Exceptions
{
    public class AnalysisWithoutDocumentException(string message = "Analysis without documents that contains GcsFilePath.") : Exception(message);
}
