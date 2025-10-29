namespace Cortex.Models.DTO;

public class PreAnalysisStageDto : StageDto
{
    public List<IndexDTO> Indexes { get; set; } = []; 
}
