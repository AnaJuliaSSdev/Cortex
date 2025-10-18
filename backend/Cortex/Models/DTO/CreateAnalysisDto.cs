using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class CreateAnalysisDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }

    [Required]
    [MaxLength(250)]
    public string Question { get; set; }
}
