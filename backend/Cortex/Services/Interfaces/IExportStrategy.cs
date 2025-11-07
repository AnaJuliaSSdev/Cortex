using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IExportStrategy
{
    /// <summary>
    /// O tipo de conteúdo (MIME type) do arquivo gerado.
    /// Ex: "application/pdf" ou "text/csv"
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// A extensão do arquivo. Ex: "pdf" ou "tex"
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Gera o arquivo de exportação como um array de bytes.
    /// </summary>
    /// <param name="analysis">A entidade Analysis completa, com todos os dados aninhados (Stages, Indexes, Categories, etc.)</param>
    /// <param name="chartImageBytes">A imagem do gráfico já convertida de Base64 para bytes</param>
    /// <returns>Um array de bytes representando o arquivo final.</returns>
    Task<byte[]> ExportAsync(Analysis analysis, byte[] chartImageBytes);
}
