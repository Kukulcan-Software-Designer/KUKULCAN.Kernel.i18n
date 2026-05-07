using ATLAS.Kernel.i18n.API.Extensions;
using ATLAS.Kernel.i18n.Application.Contracts.Requests;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.Infrastructure.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Kernel.i18n.API.Controllers;

/// <summary>
/// Translation lookup and management endpoints.
/// </summary>
/// <param name="mediator">The mediator parameter.</param>
[ApiController]
[Route("api/v1/translations")]
[Produces("application/json")]
public sealed class TranslationsController(IMediator mediator) : ControllerBase
{
    #region Commands
    /// <summary>
    /// Creates a new translation entry for a code + language combination.
    /// </summary>
    /// <param name="command">The command containing the translation details.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created translation.</returns>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateTranslationCommand command, CancellationToken ct) =>
        (await mediator.Send(command, ct)).ToCreatedResult(this, nameof(GetTranslation), new { code = command.Code, languageCode = command.LanguageCode });

    /// <summary>
    /// Updates the text of an existing translation. Resets the review status.
    /// </summary>
    /// <param name="code">The code of the translation to update.</param>
    /// <param name="languageCode">The language code of the translation to update.</param>
    /// <param name="body">The request body containing the updated translation details.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated translation.</returns>
    [HttpPut("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] string code, [FromRoute] string languageCode, [FromBody] UpdateTranslationRequest body, CancellationToken ct) =>
        (await mediator.Send(new UpdateTranslationCommand(code, languageCode, body.Text, body.Context), ct)).ToActionResult(this);

    /// <summary>
    /// Marks or unmarks a translation as reviewed by a human translator.
    /// </summary>
    /// <param name="code">The code of the translation to update.</param>
    /// <param name="languageCode">The language code of the translation to update.</param>
    /// <param name="body">The request body containing the review status.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    [HttpPatch("{code}/{languageCode}/review")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetReviewed([FromRoute] string code, [FromRoute] string languageCode, [FromBody] SetReviewedRequest body, CancellationToken ct)
        => (await mediator.Send(new SetTranslationReviewedCommand(code, languageCode, body.IsReviewed), ct)).ToNoContentResult(this);

    /// <summary>
    /// Deletes a non-English translation entry.
    /// English entries are protected and can only be removed via the bulk admin tool.
    /// </summary>
    /// <param name="code">The code of the translation to delete.</param>
    /// <param name="languageCode">The language code of the translation to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the delete operation.</returns>
    [HttpDelete("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] string code, [FromRoute] string languageCode, CancellationToken ct) =>
        (await mediator.Send(new DeleteTranslationCommand(code, languageCode), ct)).ToNoContentResult(this);

    /// <summary>
    /// Inserts or updates up to 5,000 translation entries in a single operation.
    /// Intended for import scripts and CI/CD pipelines.
    /// </summary>
    /// <param name="command">The command containing the translation entries to upsert.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The result of the bulk upsert operation.</returns>
    [HttpPost("bulk")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(BulkUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpsert([FromBody] BulkUpsertTranslationsCommand command, CancellationToken ct) =>
        (await mediator.Send(command, ct)).ToActionResult(this);
    #endregion

    #region Queries
    /// <summary>
    /// Returns the translated text for a code + language combination.
    /// Walks the BCP-47 fallback chain (e.g. es-MX → es → en) automatically.
    /// </summary>
    /// <param name="code">The translation code.</param>
    /// <param name="languageCode">The language code for the translation.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The translated text for the specified code and language.</returns>
    /// <remarks>
    /// Hot path — responses are cached for 1 hour.
    /// The response field <c>isFallback</c> indicates whether the returned text
    /// was resolved via fallback rather than the exact requested language.
    /// </remarks>
    [HttpGet("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationLookupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTranslation([FromRoute] string code, [FromRoute] string languageCode, CancellationToken ct) =>
        (await mediator.Send(new GetTranslationQuery(code, languageCode), ct)).ToActionResult(this);

    /// <summary>
    /// Returns all translations for a module and language as a flat dictionary.
    /// Gaps in the requested language are filled by the BCP-47 fallback chain.
    /// Ideal for client-side caching of a full module's string table.
    /// </summary>
    /// <param name="module">The module name.</param>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The translations for the specified module and language.</returns>
    [HttpGet("module/{module}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationMapDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModuleTranslations([FromRoute] string module, [FromRoute] string languageCode, CancellationToken ct) =>
        (await mediator.Send(new GetTranslationsByModuleQuery(module, languageCode), ct)).ToActionResult(this);

    /// <summary>
    /// Returns a paged list of translations for admin tooling.
    /// Supports optional filtering by module prefix and/or language.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="module">The module name filter.</param>
    /// <param name="languageCode">The language code filter.</param>
    /// <param name="sortBy">The sorting criteria.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    [HttpGet]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(PagedResult<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? module = null,
        [FromQuery] string? languageCode = null, [FromQuery] string? sortBy = null, CancellationToken ct = default)
    {
        var pagination = PaginationRequest.Create(page, pageSize, sortBy);
        return (await mediator.Send(new GetTranslationsPagedQuery(pagination, module, languageCode), ct)).ToActionResult(this);
    }

    /// <summary>
    /// Returns all language variants available for a given translation code.
    /// Used in admin tooling to identify which languages are missing a translation.
    /// </summary>
    /// <param name="code">The translation code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The language variants for the specified translation code.</returns>
    [HttpGet("{code}/variants")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(IReadOnlyList<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants([FromRoute] string code, CancellationToken ct) =>
        (await mediator.Send(new GetTranslationVariantsQuery(code), ct)).ToActionResult(this);
    #endregion
}
