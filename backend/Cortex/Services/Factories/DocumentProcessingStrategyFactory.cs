using Cortex.Services.Interfaces;
using Cortex.Services.Strategies;

namespace Cortex.Services.Factories;

public class DocumentProcessingStrategyFactory
{
    public IDocumentProcessingStrategy GetStrategy(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();

        return ext switch
        {
            ".txt" => new TxtDocumentProcessingStrategy(),
            ".pdf" => new PdfDocumentProcessingStrategy(),
            _ => throw new NotSupportedException($"File {ext} not supported")
        };
    }
}
