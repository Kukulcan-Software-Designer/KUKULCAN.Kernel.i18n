using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using ATLAS.Kernel.i18n.Domain.Entities;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class UpdateTranslationCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        var repo = new Mock<ITranslationRepository>();
        repo.Setup(r => r.FindAsync(It.IsAny<ATLAS.Kernel.i18n.Domain.ValueObjects.TranslationCode>(), It.IsAny<ATLAS.Kernel.Domain.ValueObjects.LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);
        var h = new UpdateTranslationCommandHandler(repo.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        var res = await h.Handle(new UpdateTranslationCommand("CRM0001","es-ES","txt"), default);
        res.IsFailure.Should().BeTrue();
    }
}

