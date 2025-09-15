using Cortex.Exceptions;
using System.Net;
using System.Text.Json;

namespace Cortex.Middlewares;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger = logger;

    private static readonly IDictionary<Type, HttpStatusCode> _exceptionStatusCodeMappings =
        new Dictionary<Type, HttpStatusCode>
        {
            { typeof(EmailAlreadyInUseException), HttpStatusCode.BadRequest },
            { typeof(InvalidCredentialsException), HttpStatusCode.BadRequest },
            { typeof(EntityNotFoundException), HttpStatusCode.NotFound },
            { typeof(AnalysisDontBelongToUserException), HttpStatusCode.BadRequest }
        };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var exceptionType = exception.GetType();
        string title;
        string detail;

        if (_exceptionStatusCodeMappings.TryGetValue(exceptionType, out var statusCode))
        {
            context.Response.StatusCode = (int)statusCode;
            title = GetTitleForStatusCode(statusCode);
            detail = exception.Message;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            title = "Internal Server Error.";
            detail = "An unexpected internal server error has occurred.";
        }

        context.Response.ContentType = "application/json";

        var problemDetails = new
        {
            Status = context.Response.StatusCode,
            Title = title,
            Detail = detail
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetTitleForStatusCode(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.NotFound => "Not Found",
            _ => "Error"
        };
}

