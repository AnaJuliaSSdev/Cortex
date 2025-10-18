using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cortex.Models;

public class Index
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; } 

    [Required]
    public int IndicatorId { get; set; }

    [ForeignKey("IndicatorId")]
    public virtual Indicator Indicator { get; set; }

    [Required]
    public int AnalysisId { get; set; }

    [ForeignKey("AnalysisId")]
    public virtual Analysis Analysis { get; set; }
}
