using Microsoft.AspNetCore.Http;

namespace Cortex.Exceptions;

public class GeminiAPIErrorException(string statusCode) : Exception($"Gemini API Error: {statusCode}");
