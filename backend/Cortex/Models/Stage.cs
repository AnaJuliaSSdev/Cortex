using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models;

public class Stage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int Order { get; set; }

    public string? PartialResult { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int AnalysisId { get; set; }

    [ForeignKey("AnalysisId")]
    public virtual Analysis Analysis { get; set; }
}
