using Cortex.Data;
using Cortex.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Index = Cortex.Models.Index;

namespace Cortex.Repositories;

public class IndexRepository(AppDbContext context) : IIndexRepository
{
    protected readonly AppDbContext _context = context;

    public async Task AddAsync(Models.Index index)
    {
        await _context.Indexes.AddAsync(index);
    }

    public async Task<List<Models.Index>> GetByIdsAsync(List<int> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return []; 
        }

        // Busca todos os Indexes cujo ID está na lista fornecida
        return await _context.Indexes
            .Where(i => ids.Contains(i.Id)) 
                                            //.Include(i => i.Indicator)
                                            // .Include(i => i.References) 
            .ToListAsync();
    }

    public List<Models.Index> GetAll()
    {
        return [.. _context.Indexes];
    }

    public async Task<Models.Index?> GetByIdAsync(int id)
    {
        return await _context.Indexes
            .Where(i => i.Id == id)
            .Include(i => i.Indicator)
            .Include(i => i.References)
            .FirstOrDefaultAsync();
    }

    public async Task<Index?> GetByIdAAndUserIdsync(int id, int userId)
    {
        return await _context.Indexes
            .Include(i => i.PreAnalysisStage.Analysis)
            .FirstOrDefaultAsync(i => i.Id == id && i.PreAnalysisStage.Analysis.UserId == userId);
    }

    public async Task<Index> UpdateIndexAsync(Index index)
    {
        _context.Indexes.Update(index);
        await _context.SaveChangesAsync();
        var updatedIndex = await GetByIdAsync(index.Id);
        return updatedIndex!;
    }

    public async Task DeleteIndexAsync(Index index)
    {
        _context.Indexes.Remove(index);
        await _context.SaveChangesAsync();
    }
}
