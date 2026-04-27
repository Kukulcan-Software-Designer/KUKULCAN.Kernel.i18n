using System.Net;
using System.Text.Json;
using Atlas.SharedKernel.Domain.Result;
using FluentValidation;

namespace ATLAS.i18n.API.Middleware;

/// <summary>
/// Global exception handler.
/// Converts unhandled exceptions to RFC 7807 Problem Details responses.
///
/// <para>
/// Most failures should never reach this middleware because handlers return
/// <see cref="Result{T}"/> and <see cref="ResultExtensions.ToActionResult{T}"/> converts
/// them to proper HTTP responses. This middleware is the last line of defence for truly
/// unexpected exceptions.
/// </para>
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false,
    };

    private readonly RequestDelegate                       _next;
    private readonly ILogger<ExceptionHandlingMiddleware>  _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate                      next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            // FluentValidation threw (not caught by ValidationBehavior — shouldn't happen)
            _logger.LogWarning(
                "FluentValidation exception on {Path}: {Errors}",
                context.Request.Path,
                string.Join(" | ", ex.Errors.Select(e => e.ErrorMessage)));

            await WriteProblemAsync(context, HttpStatusCode.UnprocessableEntity, new
            {
                type   = "https://atlas.internal/errors/validation",
                title  = "Validation.Failed",
                status = 422,
                errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, new
            {
                type   = "https://atlas.internal/errors/internal",
                title  = "Unexpected.Error",
                status = 500,
                detail = "An unexpected error occurred. Please try again later.",
            });
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext  context,
        HttpStatusCode statusCode,
        object       problem)
    {
        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, _json));
    }
}
