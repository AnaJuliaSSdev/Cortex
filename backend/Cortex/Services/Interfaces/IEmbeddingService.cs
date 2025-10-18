namespace Cortex.Services.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
    float CalculateSimilarity(float[] embedding1, float[] embedding2);
    Task<List<float[]>> SelectMostRelevantEmbeddingsToQuestion(List<float[]> embeddings, string question);
}
