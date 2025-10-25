using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IGeminiResponseHandler
{
    GeminiIndexResponse ParseResponse(string jsonResponse);
}
