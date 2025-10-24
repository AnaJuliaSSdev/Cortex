namespace Cortex.Models;

public class PreAnalysisStage : Stage
{
    public virtual ICollection<Index> Indexes { get; set; }

    public PreAnalysisStage()
    {
        Indexes = [];
    }
}
