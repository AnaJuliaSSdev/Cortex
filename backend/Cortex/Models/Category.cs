using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cortex.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)] // Aumentado para nomes de categoria potencialmente mais longos
    public string Name { get; set; }

    [Required]
    [MaxLength(1000)] // Aumentado para definições mais detalhadas
    public string Definition { get; set; }

    [Required]
    public int Frequency { get; set; } // Contagem das unidades de registro nesta categoria

    // Chave estrangeira para a etapa de exploração à qual pertence
    [Required]
    public int ExplorationOfMaterialStageId { get; set; }

    [ForeignKey("ExplorationOfMaterialStageId")]
    public virtual ExplorationOfMaterialStage ExplorationOfMaterialStage { get; set; }

    // Uma categoria contém várias unidades de registro
    public virtual ICollection<RegisterUnit> RegisterUnits { get; set; } = [];
}
