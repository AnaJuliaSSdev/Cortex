using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cortex.Models;

public class Index
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; } 

    [Required]
    public int IndicatorId { get; set; }

    [ForeignKey("IndicatorId")]
    public virtual Indicator Indicator { get; set; }

    [Required]
    public int PreAnalysisStageId { get; set; }

    [ForeignKey("PreAnalysisStageId")]
    [JsonIgnore]
    public PreAnalysisStage PreAnalysisStage { get; set; }

    public virtual ICollection<IndexReference> References { get; set; } = [];

    [JsonIgnore]
    public virtual ICollection<RegisterUnit> RegisterUnits { get; set; } = [];
}
