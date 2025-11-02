using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class UpdateIndexDto
{
    [Required]
    [MaxLength(100)]
    public string IndexName { get; set; }

    [MaxLength(500)]
    public string? IndexDescription { get; set; }

    [Required]
    [MaxLength(1000)]
    public string IndicatorName { get; set; } // O nome do Indicador
}
