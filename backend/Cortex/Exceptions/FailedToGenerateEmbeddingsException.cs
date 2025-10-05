namespace Cortex.Exceptions;

public class FailedToGenerateEmbeddingsException(string message = "Failed to generate embeddings for all chunks.") : Exception(message);