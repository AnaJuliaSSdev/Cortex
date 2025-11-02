using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories;

public class IndicatorRepository(AppDbContext context) : IIndicatorRepository
{
    protected readonly AppDbContext _context = context;

    public async Task<Indicator> AddAsync(Indicator indicator)
    {
        await _context.Indicators.AddAsync(indicator);
        return indicator;
    }

    /// <summary>
    /// Busca um indicador pelo seu nome (case-insensitive).
    /// </summary>
    public async Task<Indicator?> GetByNameAsync(string name)
    {
        return await _context.Indicators
            .FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
    }
}
