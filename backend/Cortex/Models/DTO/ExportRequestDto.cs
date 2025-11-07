using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class ExportRequestDto
{
    /// <summary>
    /// A imagem do gráfico gerado no frontend,
    /// enviada como uma string de dados Base64.
    /// Ex: "data:image/png;base64,iVBORw0KGgo..."
    /// </summary>
    [Required]
    public string ChartImageBase64 { get; set; }
}
