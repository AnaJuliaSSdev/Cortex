using Cortex.Models;

namespace Cortex.Services.Interfaces;

public interface IIndicatorService
{
    Task<Indicator> GetOrCreateIndicatorAsync(string indicatorName);
}
