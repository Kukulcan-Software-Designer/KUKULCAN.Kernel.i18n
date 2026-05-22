using ATLAS.Kernel.Abstractions.Interfaces.Infrastructure;
using ATLAS.Kernel.Domain.Result;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using ATLAS.Kernel.i18n.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class GetTranslationQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenLookupSucceeds_ReturnsDto()
    {
        var lookup = new Mock<ITranslationLookupService>();
        var cache = new Mock<ICacheService>();
        lookup.Setup(x => x.ResolveAsync(It.IsAny<ATLAS.Kernel.i18n.Domain.ValueObjects.TranslationCode>(), It.IsAny<ATLAS.Kernel.Domain.ValueObjects.LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string Text, string ActualLanguage, bool IsFallback)>.Ok(("Texto", "es-ES", false)));
        cache.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<ATLAS.Kernel.i18n.Domain.DTOs.TranslationLookupDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ATLAS.Kernel.i18n.Domain.DTOs.TranslationLookupDto?>>, TimeSpan?, CancellationToken>((_, f, _, ct) => f(ct)!);

        var handler = new GetTranslationQueryHandler(lookup.Object, cache.Object);
        var result = await handler.Handle(new GetTranslationQuery("CRM0001", "es-ES"), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Texto");
    }

    [Fact]
    public async Task Handle_WhenLookupFails_ReturnsNotFound()
    {
        var lookup = new Mock<ITranslationLookupService>();
        var cache = new Mock<ICacheService>();
        lookup.Setup(x => x.ResolveAsync(It.IsAny<ATLAS.Kernel.i18n.Domain.ValueObjects.TranslationCode>(), It.IsAny<ATLAS.Kernel.Domain.ValueObjects.LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("x", "x"));
        cache.Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task<ATLAS.Kernel.i18n.Domain.DTOs.TranslationLookupDto?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<ATLAS.Kernel.i18n.Domain.DTOs.TranslationLookupDto?>>, TimeSpan?, CancellationToken>((_, f, _, ct) => f(ct)!);

        var handler = new GetTranslationQueryHandler(lookup.Object, cache.Object);
        var result = await handler.Handle(new GetTranslationQuery("CRM0001", "es-ES"), default);
        result.IsFailure.Should().BeTrue();
    }
}
