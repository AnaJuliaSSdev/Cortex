﻿using Cortex.Data;
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

    public async Task<List<Chunk>> SearchSimilarAsync(float[] queryEmbedding, int limit = 5)
    {
        var queryVector = new Vector(queryEmbedding);

        return await _context.Chunks
            .Include(c => c.Document)
            .OrderBy(c => c.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync();
    }
}
