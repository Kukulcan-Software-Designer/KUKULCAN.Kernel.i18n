using ATLAS.i18n.Application.Languages;
using ATLAS.i18n.Application.Locales;
using ATLAS.i18n.Application.Translations.Commands;
using ATLAS.i18n.Application.Translations.Queries;
using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.API.Extensions;
using Atlas.SharedKernel.Infrastructure.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.i18n.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// TRANSLATIONS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Translation lookup and management endpoints.</summary>
[ApiController]
[Route("api/v1/translations")]
[Produces("application/json")]
public sealed class TranslationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TranslationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns the translated text for a code + language combination.
    /// Walks the BCP-47 fallback chain (e.g. es-MX → es → en) automatically.
    /// </summary>
    /// <remarks>
    /// Hot path — responses are cached for 1 hour.
    /// The response field <c>isFallback</c> indicates whether the returned text
    /// was resolved via fallback rather than the exact requested language.
    /// </remarks>
    [HttpGet("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationLookupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTranslation(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        CancellationToken  ct)
        => (await _mediator.Send(new GetTranslationQuery(code, languageCode), ct))
           .ToActionResult(this);

    /// <summary>
    /// Returns all translations for a module and language as a flat dictionary.
    /// Gaps in the requested language are filled by the BCP-47 fallback chain.
    /// Ideal for client-side caching of a full module's string table.
    /// </summary>
    [HttpGet("module/{module}/{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(TranslationMapDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModuleTranslations(
        [FromRoute] string module,
        [FromRoute] string languageCode,
        CancellationToken  ct)
        => (await _mediator.Send(new GetModuleTranslationsQuery(module, languageCode), ct))
           .ToActionResult(this);

    /// <summary>
    /// Returns a paged list of translations for admin tooling.
    /// Supports optional filtering by module prefix and/or language.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(PagedResult<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int     page           = 1,
        [FromQuery] int     pageSize       = 50,
        [FromQuery] string? module         = null,
        [FromQuery] string? languageCode   = null,
        [FromQuery] string? sortBy         = null,
        CancellationToken   ct             = default)
    {
        var pagination = PaginationRequest.Create(page, pageSize, sortBy);
        return (await _mediator.Send(
            new GetTranslationsPagedQuery(pagination, module, languageCode), ct))
           .ToActionResult(this);
    }

    /// <summary>
    /// Returns all language variants available for a given translation code.
    /// Used in admin tooling to identify which languages are missing a translation.
    /// </summary>
    [HttpGet("{code}/variants")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(IReadOnlyList<TranslationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants(
        [FromRoute] string code,
        CancellationToken  ct)
        => (await _mediator.Send(new GetTranslationVariantsQuery(code), ct))
           .ToActionResult(this);

    /// <summary>Creates a new translation entry for a code + language combination.</summary>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody]  CreateTranslationCommand command,
        CancellationToken                   ct)
        => (await _mediator.Send(command, ct))
           .ToCreatedResult(this,
               nameof(GetTranslation),
               new { code = command.Code, languageCode = command.LanguageCode });

    /// <summary>Updates the text of an existing translation. Resets the review status.</summary>
    [HttpPut("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(TranslationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        [FromBody]  UpdateTranslationRequest body,
        CancellationToken                   ct)
        => (await _mediator.Send(
            new UpdateTranslationCommand(code, languageCode, body.Text, body.Context), ct))
           .ToActionResult(this);

    /// <summary>Marks or unmarks a translation as reviewed by a human translator.</summary>
    [HttpPatch("{code}/{languageCode}/review")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetReviewed(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        [FromBody]  SetReviewedRequest body,
        CancellationToken              ct)
        => (await _mediator.Send(
            new SetTranslationReviewedCommand(code, languageCode, body.IsReviewed), ct))
           .ToNoContentResult(this);

    /// <summary>
    /// Deletes a non-English translation entry.
    /// English entries are protected and can only be removed via the bulk admin tool.
    /// </summary>
    [HttpDelete("{code}/{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] string code,
        [FromRoute] string languageCode,
        CancellationToken  ct)
        => (await _mediator.Send(new DeleteTranslationCommand(code, languageCode), ct))
           .ToNoContentResult(this);

    /// <summary>
    /// Inserts or updates up to 5,000 translation entries in a single operation.
    /// Intended for import scripts and CI/CD pipelines.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(BulkUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpsert(
        [FromBody]  BulkUpsertTranslationsCommand command,
        CancellationToken                        ct)
        => (await _mediator.Send(command, ct)).ToActionResult(this);
}

// Request bodies for routes with separate code/language path parameters
public record UpdateTranslationRequest(string Text, string? Context = null);
public record SetReviewedRequest(bool IsReviewed);

// ═══════════════════════════════════════════════════════════════════════════════
// LANGUAGES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Language management endpoints.</summary>
[ApiController]
[Route("api/v1/languages")]
[Produces("application/json")]
public sealed class LanguagesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LanguagesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns all supported languages. Pass <c>activeOnly=false</c> to include inactive ones.</summary>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetAllLanguagesQuery(activeOnly), ct)).ToActionResult(this);

    /// <summary>Returns a single language by BCP-47 code (e.g. <c>es-ES</c>).</summary>
    [HttpGet("{code}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(
        [FromRoute] string code,
        CancellationToken  ct)
        => (await _mediator.Send(new GetLanguageQuery(code), ct)).ToActionResult(this);

    /// <summary>Creates a new language.</summary>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody]  CreateLanguageCommand command,
        CancellationToken                ct)
        => (await _mediator.Send(command, ct))
           .ToCreatedResult(this, nameof(GetByCode), new { code = command.Code });

    /// <summary>Updates the display names of an existing language.</summary>
    [HttpPut("{code}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] string code,
        [FromBody]  UpdateLanguageRequest body,
        CancellationToken                ct)
        => (await _mediator.Send(
            new UpdateLanguageCommand(code, body.Name, body.NativeName), ct))
           .ToActionResult(this);

    /// <summary>Activates or deactivates a language. The default language cannot be deactivated.</summary>
    [HttpPatch("{code}/active")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetActive(
        [FromRoute] string code,
        [FromBody]  SetActiveRequest body,
        CancellationToken            ct)
        => (await _mediator.Send(new SetLanguageActiveCommand(code, body.IsActive), ct))
           .ToNoContentResult(this);

    /// <summary>
    /// Designates a language as the global default fallback.
    /// The language must be active. The previous default loses its designation.
    /// </summary>
    [HttpPatch("{code}/default")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDefault(
        [FromRoute] string code,
        CancellationToken  ct)
        => (await _mediator.Send(new SetDefaultLanguageCommand(code), ct))
           .ToNoContentResult(this);
}

public record UpdateLanguageRequest(string Name, string NativeName);
public record SetActiveRequest(bool IsActive);

// ═══════════════════════════════════════════════════════════════════════════════
// LOCALES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Locale configuration endpoints (date formats, number separators).</summary>
[ApiController]
[Route("api/v1/locales")]
[Produces("application/json")]
public sealed class LocalesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LocalesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns all locale configurations.</summary>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LocaleConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => (await _mediator.Send(new GetAllLocaleConfigurationsQuery(), ct)).ToActionResult(this);

    /// <summary>Returns the locale configuration for a specific language.</summary>
    [HttpGet("{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLanguage(
        [FromRoute] string languageCode,
        CancellationToken  ct)
        => (await _mediator.Send(new GetLocaleConfigurationQuery(languageCode), ct))
           .ToActionResult(this);

    /// <summary>Creates or updates (upsert) the locale configuration for a language.</summary>
    [HttpPut("{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert(
        [FromRoute] string languageCode,
        [FromBody]  UpsertLocaleBody body,
        CancellationToken            ct)
        => (await _mediator.Send(new UpsertLocaleConfigurationCommand(
            languageCode, body.DateFormat, body.ShortDateFormat, body.TimeFormat,
            body.DateTimeFormat, body.FirstDayOfWeek, body.DecimalSeparator,
            body.ThousandsSeparator, body.DecimalPlaces, body.CurrencyDecimalPlaces), ct))
           .ToActionResult(this);
}

public record UpsertLocaleBody(
    string DateFormat, string ShortDateFormat, string TimeFormat,
    string DateTimeFormat, string FirstDayOfWeek,
    string DecimalSeparator, string ThousandsSeparator,
    int DecimalPlaces = 2, int CurrencyDecimalPlaces = 2);

// ═══════════════════════════════════════════════════════════════════════════════
// CURRENCIES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Currency format endpoints (symbol placement, separators, negative patterns).</summary>
[ApiController]
[Route("api/v1/currencies")]
[Produces("application/json")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CurrenciesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all currency formats for a language.
    /// Each entry includes a pre-formatted example using the amount 1,234.56.
    /// </summary>
    [HttpGet("{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyFormatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLanguage(
        [FromRoute] string languageCode,
        CancellationToken  ct)
        => (await _mediator.Send(new GetCurrencyFormatsQuery(languageCode), ct))
           .ToActionResult(this);

    /// <summary>Creates or updates a currency format for a language + ISO 4217 pair.</summary>
    [HttpPut("{languageCode}/{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(CurrencyFormatDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert(
        [FromRoute] string languageCode,
        [FromRoute] string currencyCode,
        [FromBody]  UpsertCurrencyBody body,
        CancellationToken              ct)
        => (await _mediator.Send(new UpsertCurrencyFormatCommand(
            languageCode, currencyCode, body.CurrencyName, body.Symbol,
            body.SymbolPosition, body.SpaceBetweenSymbolAndAmount,
            body.DecimalSeparator, body.ThousandsSeparator,
            body.DecimalPlaces, body.NegativePattern), ct))
           .ToActionResult(this);

    /// <summary>Deletes a currency format for a language + currency pair.</summary>
    [HttpDelete("{languageCode}/{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] string languageCode,
        [FromRoute] string currencyCode,
        CancellationToken  ct)
        => (await _mediator.Send(new DeleteCurrencyFormatCommand(languageCode, currencyCode), ct))
           .ToNoContentResult(this);
}

public record UpsertCurrencyBody(
    string CurrencyName, string Symbol, string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator, string ThousandsSeparator,
    int DecimalPlaces, string NegativePattern = "-{symbol}{amount}");
