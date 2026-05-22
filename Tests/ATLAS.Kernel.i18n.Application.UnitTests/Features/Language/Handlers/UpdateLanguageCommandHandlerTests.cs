using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class UpdateLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        var repo = new Mock<ILanguageRepository>();
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Language?)null);
        var h = new UpdateLanguageCommandHandler(repo.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        var res = await h.Handle(new UpdateLanguageCommand("es-ES", "N", "NN"), default);
        res.IsFailure.Should().BeTrue();
    }
}

