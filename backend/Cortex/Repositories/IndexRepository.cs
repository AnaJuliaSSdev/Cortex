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
        await _context.SaveChangesAsync();
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
                                            // .Include(i => i.Indicator)
                                            // .Include(i => i.References) 
            .ToListAsync();
    }
}
