using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class StartAnalysisDto
{
    [Required]
    public string Question { get; set; }
}
