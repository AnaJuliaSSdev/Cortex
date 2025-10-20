using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cortex.Models;

public class IndexReference
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int IndexId { get; set; }

    [ForeignKey("IndexId")]
    public virtual Index Index { get; set; }

    // Informações da citação
    [Required]
    public string SourceDocumentUri { get; set; } // O URI do arquivo que o Gemini usou

    public int? StartIndex { get; set; } // Onde a citação começa no documento original
    public int? EndIndex { get; set; }   // Onde a citação termina

    // Aqui da pra guardar o trecho exato
    public string? QuotedContent { get; set; }
}