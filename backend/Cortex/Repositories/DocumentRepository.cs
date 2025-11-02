using Cortex.Data;
using Cortex.Models;
using Cortex.Models.Enums;
using Cortex.Repositories.Interfaces;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories;

public class DocumentRepository(AppDbContext context) : IDocumentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Document> AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents.FindAsync(id);
    }

    public async Task<IEnumerable<Document>> GetByAnalysisIdAsync(int analysisId)
    {
        return await _context.Documents
        .Include(d => d.Chunks)
        .Include(d => d.Analysis)
        .Where(d => d.AnalysisId == analysisId)
        .ToListAsync();
    }

    public async Task<long> SumTotalSizeDocumentsByAnalysisIdAsync(int analysisId, DocumentPurpose documentPurpose)
    {
        return await _context.Documents
        .Where(d => d.AnalysisId == analysisId && d.Purpose == documentPurpose)
        .SumAsync(d => d.FileSize); // FileSize deve ser 'long' no seu modelo
    }
}
