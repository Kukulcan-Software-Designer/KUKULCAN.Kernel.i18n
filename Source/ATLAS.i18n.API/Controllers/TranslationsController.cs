using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Translations.Commands;
using ATLAS.i18n.Application.Translations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.i18n.API.Controllers;

/// <summary>
/// Exposes translation lookup and management endpoints.
///
/// Query endpoints are deliberately lightweight and cache-friendly.
/// Write endpoints require the i18n.write policy (ATLAS.Admin role).
/// </summary>
[ApiController]
[Route("api/v1/translations")]
[Produces("application/json")]
public sealed class TranslationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TranslationsController(IMediator mediator) => _mediator = mediator;

    // ─── Query endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the translated text for a single code + language combination.
    /// Falls back to English if the requested language translation is unavailable.
    /// </summary>
    /// <remarks>
    /// This is the primary hot-path endpoint. Responses are cached.
    ///
    /// Example: GET /api/v1/translations/CRM0001/ES
    /// </remarks>
    [HttpGet("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationLookupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTranslation(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTranslationQuery(code, languageCode), ct);

        return Ok(result);
    }

    /// <summary>
    /// Returns all translations for a module and language as a flat dictionary.
    /// Ideal for client-side caching of an entire module's string table.
    /// </summary>
    /// <remarks>
    /// Example: GET /api/v1/translations/module/CRM/ES
    /// </remarks>
    [HttpGet("module/{module}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationMapDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByModule(
        [FromRoute] string module,
        [FromRoute] string languageCode,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTranslationsByModuleQuery(module, languageCode), ct);

        return Ok(result);
    }

    /// <summary>
    /// Returns all language variants available for a given code.
    /// Used in admin tooling to detect missing translations.
    /// </summary>
    [HttpGet("{code}/variants")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(IReadOnlyList<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants(
        [FromRoute] string code,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTranslationVariantsQuery(code), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paged list of translations for admin tooling.
    /// Supports optional filtering by module and/or language.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(PagedResult<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int     pageNumber = 1,
        [FromQuery] int     pageSize   = 50,
        [FromQuery] string? module     = null,
        [FromQuery] string? language   = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTranslationsPagedQuery(pageNumber, pageSize, module, language), ct);

        return Ok(result);
    }

    // ─── Command endpoints ────────────────────────────────────────────────────

    /// <summary>Creates a new translation entry.</summary>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTranslationCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(
            nameof(GetTranslation),
            new { code = result.Code, languageCode = result.LanguageCode },
            result);
    }

    /// <summary>Updates the text of an existing translation.</summary>
    [HttpPut("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        [FromBody] UpdateTranslationRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateTranslationCommand(code, languageCode, request.Text, request.Context), ct);

        return Ok(result);
    }

    /// <summary>Marks a translation as reviewed/unreviewed by a human translator.</summary>
    [HttpPatch("{code}/{languageCode}/review")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetReviewed(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        [FromBody] SetReviewedRequest request,
        CancellationToken ct)
    {
        await _mediator.Send(
            new MarkTranslationReviewedCommand(code, languageCode, request.IsReviewed), ct);

        return NoContent();
    }

    /// <summary>Deletes a translation for a specific code + language (non-English only).</summary>
    [HttpDelete("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        CancellationToken ct)
    {
        await _mediator.Send(new DeleteTranslationCommand(code, languageCode), ct);
        return NoContent();
    }

    /// <summary>
    /// Bulk insert or update up to 5,000 translation entries in a single call.
    /// Intended for import scripts and CI/CD pipelines.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(BulkUpsertResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpsert(
        [FromBody] BulkUpsertTranslationsCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

// ─── Request bodies (separate from MediatR commands to keep routes clean) ────

public record UpdateTranslationRequest(string Text, string? Context = null);
public record SetReviewedRequest(bool IsReviewed);
