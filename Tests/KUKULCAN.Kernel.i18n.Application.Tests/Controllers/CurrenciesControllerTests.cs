using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.API.Controllers;
using KUKULCAN.Kernel.i18n.Application.Contracts.Requests;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;
using KUKULCAN.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;
using KUKULCAN.Kernel.i18n.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Controllers;

public sealed class CurrenciesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly CurrenciesController _controller;
    public CurrenciesControllerTests() => _controller = new CurrenciesController(_mediator.Object);

    [Fact]
    public async Task Upsert_ReturnsOk()
    {
        _mediator
            .Setup(m=>
                m.Send(It.IsAny<UpsertCurrencyFormatCommand>(),It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result<CurrencyFormatDto>.Ok(D()));
        Assert.IsType<OkObjectResult>(
            await _controller.Upsert(
                "es-ES",
                "EUR",
                new UpsertCurrencyRequest(
                    "Euro",
                    "€",
                    "After",
                    true,
                    ",",
                    ".",
                    2),
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        _mediator
            .Setup(m=>
                m.Send(It.IsAny<DeleteCurrencyFormatCommand>(),It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result.Ok());
        Assert.IsType<NoContentResult>(
            await _controller.Delete("es-ES","EUR",CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetByLanguage_ReturnsOk()
    {
        _mediator
            .Setup(m=>
                m.Send(It.IsAny<GetCurrencyFormatsQuery>(),It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(Result<IReadOnlyList<CurrencyFormatDto>>.Ok(new List<CurrencyFormatDto>{D()}));
        Assert.IsType<OkObjectResult>(await _controller.GetByLanguage("es-ES",CancellationToken.None));
    }

    private static CurrencyFormatDto D() => new (
        Guid.NewGuid(),
        "es-ES",
        "EUR",
        "Euro",
        "€",
        "After",
        true,
        ",",
        ".",
        2,
        "-{symbol}{amount}",
        "1.234,56 €",
        DateTimeOffset.UtcNow,
        null);
}
