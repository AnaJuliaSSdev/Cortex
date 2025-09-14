using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class CreateAnalysisDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }
}
