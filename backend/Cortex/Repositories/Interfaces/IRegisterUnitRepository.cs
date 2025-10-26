using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IRegisterUnitRepository
{
    Task AddAsync(RegisterUnit registerUnit);
}
