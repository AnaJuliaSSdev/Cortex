using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Cortex.Models.Enums;

namespace Cortex.Models;

public class Document
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; }

    public string? Content { get; set; }

    [Required]
    public DocumentPurpose Purpose { get; set; }

    [Required]
    public DocumentType FileType { get; set; }

    public string FilePath { get; set; }

    public long FileSize { get; set; }

    [MaxLength(500)]
    public string? Source { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int AnalysisId { get; set; }

    [ForeignKey("AnalysisId")]
    public virtual Analysis Analysis { get; set; }
    public ICollection<Chunk> Chunks { get; set; } = [];
}
