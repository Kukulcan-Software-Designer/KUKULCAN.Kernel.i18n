using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Services;
using FluentAssertions;
using KUKULCAN.Kernel.Primitives.Interfaces;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class SetDefaultLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_DomainFailure_Propagates()
    {
        var domain = new Mock<ILanguageDomainService>();
        domain.Setup(d => d.SetDefaultLanguageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("x","x"));
        var h = new SetDefaultLanguageCommandHandler(domain.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        Result res = await h.Handle(
            new SetDefaultLanguageCommand("es-ES"),
            CancellationToken.None
        );
        res.IsFailure.Should().BeTrue();
    }
}

