using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using FluentAssertions;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.Primitives.Interfaces;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class UpdateLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        var repo = new Mock<ILanguageRepository>();
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Language?)null);
        var h = new UpdateLanguageCommandHandler(repo.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        Result<LanguageDto> res = await h.Handle(new UpdateLanguageCommand("es-ES", "N", "NN"), default);
        res.IsFailure.Should().BeTrue();
    }
}

