namespace Cortex.Models;

public class PreAnalysisStage : Stage
{
    List<Index> Indexes { get; set; }

    public PreAnalysisStage()
    {
        Indexes = [];
    }
}
