namespace Cortex.Models.DTO
{
    public class ViewDocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AnalysisId { get; set; }
        public List<ChunkDto> Chunks { get; set; } = [];
    }
}
