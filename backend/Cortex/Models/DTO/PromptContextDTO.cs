using Cortex.Services;

namespace Cortex.Models.DTO;

public class PromptContextDTO
{
    List<Document> Documents { get; set; }
    List<EmbeddingData> EmbeddingDatas { get; set; }
}
