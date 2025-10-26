using Cortex.Models.Enums;

namespace Cortex.Models.DTO;

public class AnalysisDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int DocumentsCount { get; set; }
    public string Question { get; set; }
    public ICollection<Stage> Stages { get; set; }
}
