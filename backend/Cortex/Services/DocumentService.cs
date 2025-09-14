using Cortex.Exceptions;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Factories;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class DocumentService(DocumentProcessingStrategyFactory factory, IDocumentRepository repository) : IDocumentService
{
    private readonly DocumentProcessingStrategyFactory _factory = factory;
    private readonly IDocumentRepository _repository = repository;

    public async Task<Document> UploadAsync(CreateDocumentDto dto, int analysisId)
    {
        IDocumentProcessingStrategy? strategy = _factory.GetStrategy(dto.File);
        Document? document = await strategy.ProcessAsync(dto.File);

        document.Title = dto.Title;
        document.Source = dto.Source;
        document.Purpose = dto.Purpose;
        document.AnalysisId = analysisId;
        document.CreatedAt = DateTime.UtcNow;

        await _repository.AddAsync(document);

        return document;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        Document? document = await _repository.GetByIdAsync(id) ?? throw new EntityNotFoundException("Document");
        return document;
    }
}
