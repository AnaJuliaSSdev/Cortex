﻿using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IChunkRepository
{
    Task AddAsync(Chunk chunk);
    Task AddRangeAsync(List<Chunk> chunks);
    Task<Chunk?> GetByIdAsync(int id);
    Task<List<Chunk>> GetByDocumentIdAsync(int documentId);
    Task<List<Chunk>> SearchSimilarAsync(float[] queryEmbedding, int limit = 5);
}
