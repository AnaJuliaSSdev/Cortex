using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public virtual Analysis Analysis { get; set; }
}
