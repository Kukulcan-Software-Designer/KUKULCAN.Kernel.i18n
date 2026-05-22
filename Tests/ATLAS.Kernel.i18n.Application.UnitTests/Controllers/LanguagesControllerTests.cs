using ATLAS.Kernel.Domain.Result;
using ATLAS.Kernel.i18n.API.Controllers;
using ATLAS.Kernel.i18n.Application.Contracts.Requests;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;
using ATLAS.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Controllers;

public sealed class LanguagesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly LanguagesController _controller;

    public LanguagesControllerTests() => _controller = new LanguagesController(_mediator.Object);

    [Fact] public async Task Create_ReturnsCreated()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateLanguageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LanguageDto>.Ok(Dto()));
        IActionResult result = await _controller.Create(new CreateLanguageCommand("es-ES", "Spanish", "Espanol"), default);
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact] public async Task Update_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateLanguageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LanguageDto>.Ok(Dto()));
        IActionResult result = await _controller.Update("es-ES", new UpdateLanguageRequest("N", "NN"), default);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact] public async Task SetActive_ReturnsNoContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SetLanguageActiveCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());
        IActionResult result = await _controller.SetActive("es-ES", new SetActiveRequest(true), default);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact] public async Task SetDefault_ReturnsNoContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SetDefaultLanguageCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok());
        IActionResult result = await _controller.SetDefault("es-ES", default);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact] public async Task GetAll_ReturnsOk()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetAllLanguagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LanguageDto>>.Ok(new List<LanguageDto>()));
        IActionResult result = await _controller.GetAll(true, default);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact] public async Task GetByCode_WhenNotFound_ReturnsNotFound()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetLanguageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("x", "x"));
        IActionResult result = await _controller.GetByCode("xx", default);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static LanguageDto Dto() => new(
        Guid.NewGuid(),
        "es-ES",
        "Spanish",
        "Espanol",
        false,
        true,
        DateTimeOffset.UtcNow,
        "test",
        null,
        null);
}
