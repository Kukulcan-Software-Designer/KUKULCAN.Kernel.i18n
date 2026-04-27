using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Currencies.Commands;
using ATLAS.i18n.Application.Currencies.Queries;
using ATLAS.i18n.Application.Languages.Commands;
using ATLAS.i18n.Application.Languages.Queries;
using ATLAS.i18n.Application.Locales.Commands;
using ATLAS.i18n.Application.Locales.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.i18n.API.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// LANGUAGES
// ═══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/languages")]
[Produces("application/json")]
public sealed class LanguagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LanguagesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns all active languages supported by the platform.</summary>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllLanguagesQuery(activeOnly), ct);
        return Ok(result);
    }

    /// <summary>Returns a single language by ISO 639-1 code (e.g. "ES").</summary>
    [HttpGet("{code}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode([FromRoute] string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLanguageQuery(code), ct);
        return Ok(result);
    }

    /// <summary>Creates a new language.</summary>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLanguageCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByCode), new { code = result.Code }, result);
    }

    /// <summary>Updates name, native name, and culture tag of an existing language.</summary>
    [HttpPut("{code}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] string code,
        [FromBody] UpdateLanguageRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateLanguageCommand(code, request.Name, request.NativeName, request.CultureTag), ct);

        return Ok(result);
    }

    /// <summary>Activates or deactivates a language.</summary>
    [HttpPatch("{code}/active")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(
        [FromRoute] string code,
        [FromBody] SetActiveRequest request,
        CancellationToken ct)
    {
        await _mediator.Send(new SetLanguageActiveCommand(code, request.IsActive), ct);
        return NoContent();
    }

    /// <summary>
    /// Sets a language as the platform default.
    /// The default language (normally English) is the fallback for all translations.
    /// </summary>
    [HttpPatch("{code}/default")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDefault(
        [FromRoute] string code,
        CancellationToken ct)
    {
        await _mediator.Send(new SetDefaultLanguageCommand(code), ct);
        return NoContent();
    }
}

public record UpdateLanguageRequest(string Name, string NativeName, string CultureTag);
public record SetActiveRequest(bool IsActive);

// ═══════════════════════════════════════════════════════════════════════════════
// LOCALES
// ═══════════════════════════════════════════════════════════════════════════════

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
    {
        var result = await _mediator.Send(new GetAllLocaleConfigurationsQuery(), ct);
        return Ok(result);
    }

    /// <summary>Returns the locale configuration for a specific language.</summary>
    /// <remarks>
    /// Includes date formats, time format, decimal separators, and number of decimal places.
    ///
    /// Example: GET /api/v1/locales/ES
    /// </remarks>
    [HttpGet("{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLanguage(
        [FromRoute] string languageCode,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetLocaleConfigurationQuery(languageCode), ct);

        return Ok(result);
    }

    /// <summary>
    /// Creates or updates the locale configuration for a language.
    /// Idempotent — safe to call repeatedly.
    /// </summary>
    [HttpPut("{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        [FromRoute] string languageCode,
        [FromBody] UpsertLocaleConfigurationRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertLocaleConfigurationCommand(
            languageCode,
            request.DateFormat,
            request.ShortDateFormat,
            request.TimeFormat,
            request.DateTimeFormat,
            request.FirstDayOfWeek,
            request.DecimalSeparator,
            request.ThousandsSeparator,
            request.DecimalPlaces,
            request.CurrencyDecimalPlaces), ct);

        return Ok(result);
    }
}

public record UpsertLocaleConfigurationRequest(
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int    DecimalPlaces         = 2,
    int    CurrencyDecimalPlaces = 2);

// ═══════════════════════════════════════════════════════════════════════════════
// CURRENCIES
// ═══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/v1/currencies")]
[Produces("application/json")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrenciesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns all currency formats configured for a specific language.
    /// The response includes a pre-formatted example ("1.234,56 €") for each currency.
    /// </summary>
    [HttpGet("{languageCode}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyFormatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLanguage(
        [FromRoute] string languageCode,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetCurrencyFormatsQuery(languageCode), ct);

        return Ok(result);
    }

    /// <summary>Creates or updates a currency format for a specific language.</summary>
    /// <remarks>
    /// Key fields:
    /// - **symbolPosition**: "Before" ($1,234.56) or "After" (1.234,56 €)
    /// - **spaceBetweenSymbolAndAmount**: true produces "1.234,56 €", false "$1,234.56"
    /// - **negativePattern**: template with {symbol} and {amount} tokens, e.g. "({symbol}{amount})"
    /// </remarks>
    [HttpPut("{languageCode}/{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(CurrencyFormatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        [FromRoute] string languageCode,
        [FromRoute] string currencyCode,
        [FromBody] UpsertCurrencyFormatRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertCurrencyFormatCommand(
            languageCode,
            currencyCode,
            request.CurrencyName,
            request.Symbol,
            request.SymbolPosition,
            request.SpaceBetweenSymbolAndAmount,
            request.DecimalSeparator,
            request.ThousandsSeparator,
            request.DecimalPlaces,
            request.NegativePattern), ct);

        return Ok(result);
    }

    /// <summary>Deletes a currency format for a specific language + currency pair.</summary>
    [HttpDelete("{languageCode}/{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] string languageCode,
        [FromRoute] string currencyCode,
        CancellationToken ct)
    {
        await _mediator.Send(new DeleteCurrencyFormatCommand(languageCode, currencyCode), ct);
        return NoContent();
    }
}

public record UpsertCurrencyFormatRequest(
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool   SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int    DecimalPlaces,
    string NegativePattern = "-{symbol}{amount}");
