using ATLAS.Kernel.i18n.API.Extensions;
using ATLAS.Kernel.i18n.Application.Contracts.Requests;
using ATLAS.Kernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;
using ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;
using ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using ATLAS.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Kernel.i18n.API.Controllers;

/// <summary>
/// Locale configuration endpoints (date formats, number separators).
/// /// </summary>
/// <param name="mediator"></param>
[ApiController]
[Route("api/v1/locales")]
[Produces("application/json")]
public sealed class LocalesController(IMediator mediator) : ControllerBase
{
    #region Queries
    /// <summary>
    /// <summary>Returns all locale configurations.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LocaleConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        (await mediator.Send(new GetAllLocaleConfigurationsQuery(), ct)).ToActionResult(this);

    /// <summary>
    /// Returns the locale configuration for a specific language.
    /// </summary>
    /// <param name="languageCode"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLanguage([FromRoute] string languageCode, CancellationToken ct) =>
        (await mediator.Send(new GetLocaleConfigurationQuery(languageCode), ct)).ToActionResult(this);
    #endregion

    #region Commands
    /// <summary>
    /// Creates or updates (upsert) the locale configuration for a language.
    /// </summary>
    /// <param name="languageCode"></param>
    /// <param name="body"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPut("{languageCode}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LocaleConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert([FromRoute] string languageCode, [FromBody] UpsertLocaleRequest body, CancellationToken ct) =>
        (await mediator.Send(new UpsertLocaleConfigurationCommand(
            languageCode, body.DateFormat, body.ShortDateFormat, body.TimeFormat,
            body.DateTimeFormat, body.FirstDayOfWeek, body.DecimalSeparator,
            body.ThousandsSeparator, body.DecimalPlaces, body.CurrencyDecimalPlaces), ct)).ToActionResult(this);
    #endregion
}
