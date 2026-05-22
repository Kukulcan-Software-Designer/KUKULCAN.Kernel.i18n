using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class DeleteTranslationCommandHandlerTests
{
    [Fact]
    public async Task Handle_EnglishDelete_ReturnsConflict()
    {
        var h = new DeleteTranslationCommandHandler(Mock.Of<ITranslationRepository>(), Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        var res = await h.Handle(new DeleteTranslationCommand("CRM0001","en-US"), default);
        res.IsFailure.Should().BeTrue();
    }
}

