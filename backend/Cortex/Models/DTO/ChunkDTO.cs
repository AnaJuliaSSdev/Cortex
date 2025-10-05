namespace Cortex.Models.DTO
{
    public class ChunkDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public string EmbeddingPreview { get; set; } = "Não gerado"; // Apenas uma prévia, não o vetor inteiro
    }
}
