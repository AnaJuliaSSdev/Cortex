using System.ComponentModel.DataAnnotations;

namespace Cortex.Models;

public class Indicator
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Name { get; set; }
}
