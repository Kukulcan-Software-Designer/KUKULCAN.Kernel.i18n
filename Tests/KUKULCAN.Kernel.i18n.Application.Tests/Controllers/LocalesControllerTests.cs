using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.API.Controllers;
using KUKULCAN.Kernel.i18n.Application.Contracts.Requests;
using KUKULCAN.Kernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;
using KUKULCAN.Kernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;
using KUKULCAN.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using KUKULCAN.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Controllers;

public sealed class LocalesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly LocalesController _controller;
    public LocalesControllerTests() => _controller = new LocalesController(_mediator.Object);

    [Fact]
    public async Task Upsert_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<UpsertLocaleConfigurationCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<LocaleConfigurationDto>.Ok(D())); Assert.IsType<OkObjectResult>(await _controller.Upsert("es-ES",new UpsertLocaleRequest("d","d","t","dt","Monday",".",",",2,2),default));
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetAllLocaleConfigurationsQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyList<LocaleConfigurationDto>>.Ok(new List<LocaleConfigurationDto>{D()})); Assert.IsType<OkObjectResult>(await _controller.GetAll(default));
    }

    [Fact]
    public async Task GetByLanguage_NotFound_ReturnsNotFound()
    {
        _mediator.Setup(m=>m.Send(It.IsAny<GetLocaleConfigurationQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(Error.NotFound("x","x")); Assert.IsType<NotFoundObjectResult>(await _controller.GetByLanguage("xx",default));
    }

    private static LocaleConfigurationDto D() => new(
        "es-ES",
        "d",
        "d",
        "t",
        "dt",
        "Monday",
        ".",
        ",",
        2,
        2,
        DateTimeOffset.UtcNow,
        null);
}
