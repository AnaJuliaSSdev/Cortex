using Cortex.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace Cortex.Models;

public class Analysis
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [Required]
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Draft;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Foreign Key
    [Required]
    public int UserId { get; set; }

    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [Required]
    public string Question { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = [];
    public virtual ICollection<Stage> Stages { get; set; } = [];
}
