using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.API.Extensions;
using KUKULCAN.Kernel.i18n.Application.Contracts.Requests;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KUKULCAN.Kernel.i18n.API.Controllers;

/// <summary>
/// Currency format endpoints (symbol placement, separators, negative patterns).
/// </summary>
/// <param name="mediator">The mediator instance for handling requests.</param>
[ApiController]
[Route("api/v1/currencies/{languageCode}")]
[Produces("application/json")]
public sealed class CurrenciesController(IMediator mediator) : ControllerBase
{
    #region Commands
    /// <summary>
    /// Creates or updates a currency format for a language + currency pair.
    /// </summary>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="currencyCode">The currencyCode parameter.</param>
    /// <param name="body">The body parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    [HttpPut("{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(CurrencyFormatDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert([FromRoute] string languageCode, [FromRoute] string currencyCode,
        [FromBody] UpsertCurrencyRequest body, CancellationToken ct) =>
        (await mediator.Send(new UpsertCurrencyFormatCommand(languageCode, currencyCode, body.CurrencyName, body.Symbol,
            body.SymbolPosition, body.SpaceBetweenSymbolAndAmount, body.DecimalSeparator, body.ThousandsSeparator,
            body.DecimalPlaces, body.NegativePattern), ct)).ToActionResult(this);

    /// <summary>
    /// Deletes a currency format for a language + currency pair.
    /// </summary>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="currencyCode">The currencyCode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    [HttpDelete("{currencyCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] string languageCode, [FromRoute] string currencyCode, CancellationToken ct) =>
        (await mediator.Send(new DeleteCurrencyFormatCommand(languageCode, currencyCode), ct)).ToNoContentResult(this);
    #endregion

    #region Queries
    /// <summary>
    /// Returns all currency formats for a language.
    /// Each entry includes a pre-formatted example using the amount 1,234.56.
    /// </summary>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    [HttpGet("")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<CurrencyFormatDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLanguage([FromRoute] string languageCode, CancellationToken ct) =>
        (await mediator.Send(new GetCurrencyFormatsQuery(languageCode), ct)).ToActionResult(this);
    #endregion
}
