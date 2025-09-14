
using Cortex.Models;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Strategies;

public class TxtDocumentProcessingStrategy : IDocumentProcessingStrategy
{
    public async Task<Document> ProcessAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        using var reader = new StreamReader(file.OpenReadStream());
        string content = await reader.ReadToEndAsync();

        return new Document
        {
            FileType = DocumentType.Text,
            FileName = file.FileName,
            Content = content,
            FileData = fileBytes
        };
    }
}
