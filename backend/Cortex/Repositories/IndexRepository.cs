using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;

namespace Cortex.Repositories;

public class IndexRepository(AppDbContext context) : IIndexRepository
{
    protected readonly AppDbContext _context = context;

    public async Task AddAsync(Models.Index index)
    {
        await _context.Indexes.AddAsync(index);
        await _context.SaveChangesAsync();
    }
}
