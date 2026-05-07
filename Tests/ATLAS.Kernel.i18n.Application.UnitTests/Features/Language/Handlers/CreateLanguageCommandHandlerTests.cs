using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class CreateLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDuplicate_ReturnsConflict()
    {
        var repo = new Mock<ILanguageRepository>();
        repo.Setup(r => r.ExistsByCodeAsync("es-ES", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateLanguageCommandHandler(repo.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());

        var result = await handler.Handle(new CreateLanguageCommand("es-ES", "Spanish", "Espanol"), default);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValid_PersistsAndInvalidatesCache()
    {
        var repo = new Mock<ILanguageRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<ICacheService>();
        repo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreateLanguageCommandHandler(repo.Object, uow.Object, cache.Object);
        var result = await handler.Handle(new CreateLanguageCommand("es-ES", "Spanish", "Espanol"), default);

        result.IsSuccess.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Language>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

