using Cortex.Models;

namespace Cortex.Util;

public static class Util
{
    /// <summary>
    /// Encontra o GCS URI de um documento com base no nome do arquivo.
    /// </summary>
    public static string FindGcsUriFromFileName(IEnumerable<Document> allDocuments, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        // Tenta encontrar pelo 'FileName' (ex: "entrevista.pdf")
        var doc = allDocuments.FirstOrDefault(d =>
            d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
            d.Title.Equals(fileName, StringComparison.OrdinalIgnoreCase)
        );

        return doc?.GcsFilePath; // Retorna o GCS URI ou null
    }

    /// <summary>
    /// Remove os caracteres indesejados do começo e do final da resposta json do gemini/vertex
    /// </summary>
    public static string SanitizeGeminiJsonResponse(string jsonResponse)
    {
        jsonResponse = jsonResponse.Trim();

        if (jsonResponse.StartsWith("```json"))
        {
            jsonResponse = jsonResponse.Substring(7); // Remove "```json" (7 caracteres)
        }
        if (jsonResponse.EndsWith("```"))
        {
            jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3); // Remove "```" (3 caracteres)
        }

        jsonResponse = jsonResponse.Trim();
        return jsonResponse;
    }


}
