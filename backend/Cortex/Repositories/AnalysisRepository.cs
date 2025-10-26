using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories
{
    public class AnalysisRepository(AppDbContext context) : IAnalysisRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Analysis?> RevertLastStageAsync(int analysisId)
        {
            var analysis = await _context.Analyses
                .Include(a => a.Stages)
                .FirstOrDefaultAsync(a => a.Id == analysisId);

            if (analysis == null)
            {
                return null; 
            }

            var lastStage = analysis.Stages.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            if (lastStage != null)
            {
                _context.Stages.Remove(lastStage);
                if (analysis.Stages.Count == 1) // Se a contagem ANTES da remoção era 1...
                {
                    analysis.Status = Models.Enums.AnalysisStatus.Draft;
                } 

                analysis.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return analysis;
        }

        public async Task<Analysis?> GetByIdAsync(int id)
        {
            var analysis = await _context.Analyses
                 .Include(a => a.User)
                 .Include(a => a.Stages)
                 .FirstOrDefaultAsync(a => a.Id == id);

            if (analysis == null)
                return null;

            await _context.Entry(analysis)
                .Collection(a => a.Stages)
                .Query()
                .OfType<PreAnalysisStage>()
                .Include(p => p.Indexes)
                    .ThenInclude(i => i.Indicator)
                .Include(p => p.Indexes)
                    .ThenInclude(i => i.References)
                .LoadAsync();

            return analysis;
        }

        public async Task<Analysis?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Analyses
                .Include(a => a.User)
                .Include(a => a.Documents)
                .Include(a => a.Stages)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Analysis>> GetByUserIdAsync(int userId)
        {
            return await _context.Analyses
                .Include(a => a.Documents)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Analysis> CreateAsync(Analysis analysis)
        {
            _context.Analyses.Add(analysis);
            await _context.SaveChangesAsync();
            return analysis;
        }

        public async Task<Analysis> UpdateAsync(Analysis analysis)
        {
            analysis.UpdatedAt = DateTime.UtcNow;
            _context.Analyses.Update(analysis);
            await _context.SaveChangesAsync();
            return analysis;
        }

        public async Task DeleteAsync(int id)
        {
            var analysis = await _context.Analyses.FindAsync(id);
            if (analysis != null)
            {
                _context.Analyses.Remove(analysis);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Analyses.AnyAsync(a => a.Id == id);
        }

        public async Task<bool> BelongsToUserAsync(int analysisId, int userId)
        {
            return await _context.Analyses
                .AnyAsync(a => a.Id == analysisId && a.UserId == userId);
        }
    }
}
