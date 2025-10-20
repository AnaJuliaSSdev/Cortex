using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models;

public abstract class Stage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int AnalysisId { get; set; }

    [ForeignKey("AnalysisId")]
    public virtual Analysis Analysis { get; set; }

    protected Stage()
    {
        CreatedAt = DateTime.UtcNow;
    }
}
