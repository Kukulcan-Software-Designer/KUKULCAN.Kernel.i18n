using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.Primitives.Interfaces;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class DeleteTranslationCommandHandlerTests
{
    [Fact]
    public async Task Handle_EnglishDelete_ReturnsConflict()
    {
        var h = new DeleteTranslationCommandHandler(
            Mock.Of<ITranslationRepository>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<ICacheService>()
        );
        Result res = await h.Handle(
            new DeleteTranslationCommand(
                "CRM0001",
                "en-US"
            ),
            CancellationToken.None
        );
        res.IsFailure.Should().BeTrue();
    }
}

