using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface ICategoryRepository
{
    Task AddAsync(Category category);
}
