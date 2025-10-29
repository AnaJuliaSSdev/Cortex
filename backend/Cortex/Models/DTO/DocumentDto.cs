using Cortex.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class DocumentDto
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string FileName { get; set; }

    public DocumentType FileType { get; set; }
}
