using KUKULCAN.Kernel.Abstractions.Interfaces.Infrastructure;
using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Services;
using FluentAssertions;
using KUKULCAN.Kernel.Domain.ValueObjects;
using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Domain.ValueObjects;
using Moq;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Translations.Handlers;

public sealed class GetTranslationQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenLookupSucceeds_ReturnsDto()
    {
        var lookup = new Mock<ITranslationLookupService>();
        var cache = new Mock<ICacheService>();
        lookup.Setup(x =>
                x.ResolveAsync(
                    It.IsAny<TranslationCode>(),
                    It.IsAny<LanguageCode>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<(string Text, string ActualLanguage, bool IsFallback)>
                .Ok((
                    "Texto",
                    "es-ES",
                    false)
                )
        );
        cache.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<TranslationLookupDto?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()
            )
        ) .Returns<string, Func<CancellationToken, Task<TranslationLookupDto?>>, TimeSpan?, CancellationToken>((_, f, _, ct) => f(ct)!);

        var handler = new GetTranslationQueryHandler(lookup.Object, cache.Object);
        Result<TranslationLookupDto> result = await handler.Handle(
            new GetTranslationQuery(
                "CRM0001",
                "es-ES"
            ), CancellationToken.None
        );
        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Texto");
    }

    [Fact]
    public async Task Handle_WhenLookupFails_ReturnsNotFound()
    {
        var lookup = new Mock<ITranslationLookupService>();
        var cache = new Mock<ICacheService>();
        lookup.Setup(x =>
                x.ResolveAsync(
                    It.IsAny<TranslationCode>(),
                    It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Error.NotFound("x", "x"));
        cache.Setup(x =>
                x.GetOrCreateAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<CancellationToken, Task<TranslationLookupDto?>>>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()
                )
            ).Returns<string, Func<CancellationToken, Task<TranslationLookupDto?>>, TimeSpan?, CancellationToken>((_, f, _, ct) => f(ct)!);

        var handler = new GetTranslationQueryHandler(lookup.Object, cache.Object);
        Result<TranslationLookupDto> result = await handler.Handle(
            new GetTranslationQuery(
                "CRM0001",
                "es-ES"
            ),
            CancellationToken.None
        );
        result.IsFailure.Should().BeTrue();
    }
}
