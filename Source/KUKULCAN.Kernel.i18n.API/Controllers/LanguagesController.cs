using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.API.Extensions;
using KUKULCAN.Kernel.i18n.Application.Contracts.Requests;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KUKULCAN.Kernel.i18n.API.Controllers;

/// <summary>
/// Language management endpoints.
/// /// </summary>
/// <param name="mediator">The mediator instance for handling requests.</param>
[ApiController]
[Route("api/v1/languages")]
[Produces("application/json")]
public sealed class LanguagesController(IMediator mediator) : ControllerBase
{
    #region Commands
    /// <summary>
    /// Add a new language.
    /// </summary>
    /// <param name="command">The command containing the details of the language to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created language.</returns>
    [HttpPost]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateLanguageCommand command, CancellationToken ct) =>
        (await mediator.Send(command, ct)).ToCreatedResult(this, nameof(GetByCode), new { code = command.Code });

    /// <summary>
    /// Updates the display names of an existing language.
    /// </summary>
    /// <param name="code">The BCP-47 code of the language to update.</param>
    /// <param name="body">The request containing the new display names.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated language.</returns>
    [HttpPut("{code}")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] string code, [FromBody] UpdateLanguageRequest body, CancellationToken ct) =>
        (await mediator.Send(new UpdateLanguageCommand(code, body.Name, body.NativeName), ct)).ToActionResult(this);

    /// <summary>
    /// Activates or deactivates a language. The default language cannot be deactivated.
    /// </summary>
    /// <param name="code">The BCP-47 code of the language to activate or deactivate.</param>
    /// <param name="body">The request containing the active status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
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
    /// <param name="code">The BCP-47 code of the language to set as default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPatch("{code}/default")]
    [Authorize(Policy = "i18n.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDefault([FromRoute] string code, CancellationToken ct) =>
        (await mediator.Send(new SetDefaultLanguageCommand(code), ct)).ToNoContentResult(this);
    #endregion

    #region Queries
    /// <summary>
    /// Returns all supported languages. Pass <c>activeOnly=false</c> to include inactive ones.
    /// </summary>
    /// <param name="activeOnly">Whether to include only active languages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of languages.</returns>
    [HttpGet]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(IReadOnlyList<LanguageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        (await mediator.Send(new GetAllLanguagesQuery(activeOnly), ct)).ToActionResult(this);

    /// <summary>
    /// Returns a single language by BCP-47 code (e.g. <c>es-ES</c>).
    /// </summary>
    /// <param name="code">The BCP-47 code of the language.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The language matching the specified code.</returns>
    [HttpGet("{code}")]
    [Authorize(Policy = "i18n.read")]
    [ProducesResponseType(typeof(LanguageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode([FromRoute] string code, CancellationToken ct) =>
        (await mediator.Send(new GetLanguageQuery(code), ct)).ToActionResult(this);
    #endregion
}
