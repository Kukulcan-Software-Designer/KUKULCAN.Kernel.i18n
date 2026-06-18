using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Application.Common;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using KUKULCAN.Kernel.Primitives.Interfaces;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class SetLanguageActiveCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenLanguageNotFound_ReturnsNotFound()
    {
        var repo = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        repo.Setup(r => r.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Language?)null);

        var handler = new SetLanguageActiveCommandHandler(repo.Object, uow.Object, cache.Object);
        var result = await handler.Handle(new SetLanguageActiveCommand("es-ES", true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenActivate_SavesAndInvalidatesCache()
    {
        var repo = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        Domain.Entities.Language lang = Domain.Entities.Language.Create(
            Guid.NewGuid(),
            "es-ES",
            "Spanish",
            "Espanol").Value;

        lang.Deactivate();
        repo.Setup(r => r.GetByCodeAsync("es-ES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(lang);

        var handler = new SetLanguageActiveCommandHandler(repo.Object, uow.Object, cache.Object);
        Result result = await handler.Handle(new SetLanguageActiveCommand("es-ES", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(
                I18NCacheKeys.Language("es-ES"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
