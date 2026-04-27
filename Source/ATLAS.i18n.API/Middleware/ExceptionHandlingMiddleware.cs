using System.Net;
using System.Text.Json;
using ATLAS.i18n.Domain.Exceptions;
using FluentValidation;

namespace ATLAS.i18n.API.Middleware;

/// <summary>
/// Centralized exception handler that converts domain and application exceptions
/// into RFC 7807 Problem Details responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate                    _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false,
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
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
            _logger.LogWarning("Validation error on {Path}: {Errors}",
                context.Request.Path,
                string.Join(" | ", ex.Errors.Select(e => e.ErrorMessage)));

            await WriteProblemAsync(context, HttpStatusCode.BadRequest, new
            {
                type    = "https://atlas.internal/errors/validation",
                title   = "Validation Error",
                status  = 400,
                errors  = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }
        catch (TranslationNotFoundException ex)
        {
            _logger.LogInformation("Translation not found: {Code}/{Lang}",
                ex.TranslationCode, ex.LanguageCode);

            await WriteProblemAsync(context, HttpStatusCode.NotFound, new
            {
                type   = "https://atlas.internal/errors/translation-not-found",
                title  = "Translation Not Found",
                status = 404,
                detail = ex.Message,
            });
        }
        catch (LanguageNotFoundException ex)
        {
            _logger.LogInformation("Language not found: {Code}", ex.LanguageCode);

            await WriteProblemAsync(context, HttpStatusCode.NotFound, new
            {
                type   = "https://atlas.internal/errors/language-not-found",
                title  = "Language Not Found",
                status = 404,
                detail = ex.Message,
            });
        }
        catch (LocaleConfigurationNotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, new
            {
                type   = "https://atlas.internal/errors/locale-not-found",
                title  = "Locale Configuration Not Found",
                status = 404,
                detail = ex.Message,
            });
        }
        catch (DuplicateTranslationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict, new
            {
                type   = "https://atlas.internal/errors/duplicate-translation",
                title  = "Duplicate Translation",
                status = 409,
                detail = ex.Message,
            });
        }
        catch (I18nDomainException ex)
        {
            _logger.LogWarning("Domain rule violation: {Message}", ex.Message);

            await WriteProblemAsync(context, HttpStatusCode.BadRequest, new
            {
                type   = "https://atlas.internal/errors/domain-rule-violation",
                title  = "Domain Rule Violation",
                status = 400,
                detail = ex.Message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, new
            {
                type   = "https://atlas.internal/errors/internal",
                title  = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred. Please try again later.",
            });
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        object problem)
    {
        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
