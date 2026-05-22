using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.Domain.Result;
using ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;
using ATLAS.Kernel.i18n.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Language.Handlers;

public sealed class SetDefaultLanguageCommandHandlerTests
{
    [Fact]
    public async Task Handle_DomainFailure_Propagates()
    {
        var domain = new Mock<ILanguageDomainService>();
        domain.Setup(d => d.SetDefaultLanguageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("x","x"));
        var h = new SetDefaultLanguageCommandHandler(domain.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICacheService>());
        var res = await h.Handle(new SetDefaultLanguageCommand("es-ES"), default);
        res.IsFailure.Should().BeTrue();
    }
}

