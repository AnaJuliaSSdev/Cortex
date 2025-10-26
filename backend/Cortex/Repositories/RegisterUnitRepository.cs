using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories.Interfaces;

namespace Cortex.Repositories;

public class RegisterUnitRepository(AppDbContext context) : IRegisterUnitRepository
{
    private readonly AppDbContext _context = context; 
    public async Task AddAsync(RegisterUnit registerUnit)
    {
        await _context.RegisterUnits.AddAsync(registerUnit);
        await _context.SaveChangesAsync();
    }
}
