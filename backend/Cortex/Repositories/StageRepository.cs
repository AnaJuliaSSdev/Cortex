using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;

namespace Cortex.Repositories;

public class StageRepository(AppDbContext context) : IStageRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Stage> AddAsync(Stage stage)
    {
        await _context.Stages.AddAsync(stage);
        await _context.SaveChangesAsync();
        return stage;
    }
}
