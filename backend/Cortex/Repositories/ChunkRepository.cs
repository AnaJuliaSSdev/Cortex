using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Cortex.Repositories;

public class ChunkRepository : IChunkRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChunkRepository> _logger;

    public ChunkRepository(AppDbContext context, ILogger<ChunkRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(Chunk chunk)
    {
        await _context.Chunks.AddAsync(chunk);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(List<Chunk> chunks)
    {
        await _context.Chunks.AddRangeAsync(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task<Chunk?> GetByIdAsync(int id)
    {
        return await _context.Chunks
            .Include(c => c.Document)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Chunk>> GetByDocumentIdAsync(int documentId)
    {
        return await _context.Chunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync();
    }

    public async Task<List<Chunk>> SearchSimilarAsync(float[] queryEmbedding, int limit = 5, int? documentId = null, int? analysisId = null)
    {
        var queryVector = new Vector(queryEmbedding);

        var query = _context.Chunks
            .Include(c => c.Document)
            .AsQueryable();


        if (documentId.HasValue)
        {
            query = query.Where(c => c.DocumentId == documentId.Value);
        }

        else if (analysisId.HasValue)
        {
            query = query.Where(c => c.Document.AnalysisId == analysisId.Value);
        }

        return await query
            .OrderBy(c => c.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Chunk>> SearchSimilarByDocumentIdAsync(int documentId, float[] queryEmbedding, int limit = 10)
    {
        var queryVector = new Vector(queryEmbedding);

        return await _context.Chunks
            .Include(c => c.Document)
            .Where(c => c.DocumentId == documentId) 
            .OrderBy(c => c.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync();
    }
}
