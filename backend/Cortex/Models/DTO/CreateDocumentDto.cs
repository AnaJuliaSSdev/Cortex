using Cortex.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class CreateDocumentDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [Required]
    public DocumentPurpose Purpose { get; set; }

    [Required]
    public IFormFile File { get; set; }

    [MaxLength(500)]
    public string? Source { get; set; }
}