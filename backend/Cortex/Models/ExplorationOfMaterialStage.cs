namespace Cortex.Models;

public class ExplorationOfMaterialStage : Stage
{
    // Uma etapa de exploração contém várias categorias
    public virtual ICollection<Category> Categories { get; set; }
}
