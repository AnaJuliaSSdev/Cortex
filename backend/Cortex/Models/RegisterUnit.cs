using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cortex.Models;

public class RegisterUnit
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "text")] 
    public string Text { get; set; } // Trecho exato do documento

    [Required]
    [MaxLength(1000)] // Para GCS URIs
    public string SourceDocumentUri { get; set; } // URI do documento de origem

    [MaxLength(20)] // Suficiente para números de página
    public string Page { get; set; }

    [MaxLength(1000)] // Para justificativas
    public string Justification { get; set; }

    // Chave estrangeira para a categoria à qual pertence
    [Required]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [JsonIgnore]
    public virtual Category Category { get; set; }

    // Propriedade de navegação para o relacionamento Muitos-para-Muitos com Index
    public virtual ICollection<Index> FoundIndices { get; set; } = [];
}
