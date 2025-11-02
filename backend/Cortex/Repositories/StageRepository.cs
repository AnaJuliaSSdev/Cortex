using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories;

public class StageRepository(AppDbContext context) : IStageRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Stage> AddAsync(Stage stage)
    {
        await _context.Stages.AddAsync(stage);
        return stage;
    }

    public async Task<Stage?> GetByIdAndUserIdAsync(int stageId, int userId)
    {
        return await _context.Stages
             .Include(s => s.Analysis)
             .FirstOrDefaultAsync(s => s.Id == stageId && s.Analysis.UserId == userId);
    }
}
