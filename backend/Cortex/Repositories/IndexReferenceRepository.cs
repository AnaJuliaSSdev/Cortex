using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;

namespace Cortex.Repositories;

public class IndexReferenceRepository(AppDbContext context) : IIndexReferenceRepository
{
    protected readonly AppDbContext _context = context;

    public async Task AddAsync(IndexReference indexReference)
    {
        await _context.IndexReferences.AddAsync(indexReference);
        await _context.SaveChangesAsync();
    }
}
