using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IGeminiResponseHandler
{
    T ParseResponse<T>(string jsonResponse) where T : class;
}
