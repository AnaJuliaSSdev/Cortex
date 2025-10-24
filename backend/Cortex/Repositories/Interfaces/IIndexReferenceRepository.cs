using Cortex.Models;

namespace Cortex.Repositories.Interfaces;

public interface IIndexReferenceRepository
{
    Task AddAsync(IndexReference indexReference);
}
