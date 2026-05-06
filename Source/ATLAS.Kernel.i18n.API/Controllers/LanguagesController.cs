using ATLAS.Kernel.i18n.API.Extensions;
using ATLAS.Kernel.i18n.Application.Contracts.Requests;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using ATLAS.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Kernel.i18n.API.Controllers;

/// <summary>
/// Language management endpoints.
/// /// </summary>
/// <param name="mediator"></param>
[ApiController]
[Route("api/v1/languages")]
[Produces("application/json")]
public sealed class LanguagesController(IMediator mediator) : ControllerBase
{
    #region Queries
    /// <summary>
    /// Returns all supported languages. Pass <c>activeOnly=false</c> to include inactive ones.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        (await mediator.Send(new GetAllLanguagesQuery(activeOnly), ct)).ToActionResult(this);

    /// <summary>Returns a single language by BCP-47 code (e.g. <c>es-ES</c>).</summary>
    [HttpGet("{code}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode([FromRoute] string code, CancellationToken ct) =>
        (await mediator.Send(new GetLanguageQuery(code), ct)).ToActionResult(this);
    #endregion

    #region Commands
    /// <summary>
    /// Creates a new language.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateLanguageCommand command, CancellationToken ct) =>
        (await mediator.Send(command, ct)).ToCreatedResult(this, nameof(GetByCode), new { code = command.Code });

    /// <summary>
    /// Updates the display names of an existing language.
    /// </summary>
    [HttpPut("{code}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] string code, [FromBody] UpdateLanguageRequest body, CancellationToken ct) =>
        (await mediator.Send(new UpdateLanguageCommand(code, body.Name, body.NativeName), ct)).ToActionResult(this);

    /// <summary>
    /// Activates or deactivates a language. The default language cannot be deactivated.
    /// </summary>
    [HttpPatch("{code}/active")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetActive([FromRoute] string code, [FromBody] SetActiveRequest body, CancellationToken ct) =>
        (await mediator.Send(new SetLanguageActiveCommand(code, body.IsActive), ct)).ToNoContentResult(this);

    /// <summary>
    /// Designates a language as the global default fallback.
    /// The language must be active. The previous default loses its designation.
    /// </summary>
    [HttpPatch("{code}/default")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDefault([FromRoute] string code, CancellationToken ct) =>
        (await mediator.Send(new SetDefaultLanguageCommand(code), ct)).ToNoContentResult(this);
    #endregion
}
