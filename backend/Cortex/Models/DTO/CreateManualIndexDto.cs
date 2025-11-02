using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class CreateManualIndexDto
{
    [Required]
    public int PreAnalysisStageId { get; set; }

    [Required]
    [MaxLength(100)]
    public string IndexName { get; set; }

    [MaxLength(500)]
    public string? IndexDescription { get; set; }

    [Required]
    [MaxLength(1000)]
    public string IndicatorName { get; set; }
}
