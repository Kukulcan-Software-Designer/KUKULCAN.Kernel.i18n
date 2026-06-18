using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using KUKULCAN.Kernel.i18n.Domain.Entities;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.Domain.ValueObjects;
using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Domain.ValueObjects;
using KUKULCAN.Kernel.Primitives.Interfaces;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class UpdateTranslationCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        var repo = new Mock<ITranslationRepository>();
        repo.Setup(r =>
                r.FindAsync(
                    It.IsAny<TranslationCode>(),
                    It.IsAny<LanguageCode>(),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync((Translation?)null);
        var h = new UpdateTranslationCommandHandler(repo.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        Result<TranslationDto> res = await h.Handle(
            new UpdateTranslationCommand(
                "CRM0001",
                "es-ES",
                "txt"
            ),
            CancellationToken.None
        );
        res.IsFailure.Should().BeTrue();
    }
}

