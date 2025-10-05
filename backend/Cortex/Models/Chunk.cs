using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Pgvector;

namespace Cortex.Models;

public class Chunk
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [Required]
    public int ChunkIndex { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public Vector Embedding { get; set; } = new Vector(Array.Empty<float>());

    public Dictionary<string, object> Metadata { get; set; } = new();

    public int TokenCount { get; set; }

    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    //public string SourceType { get; set; } // pré prompt ou prompt de fato
}
