using ATLAS.Kernel.i18n.Application.Common.Interfaces;
using ATLAS.Kernel.i18n.Application.Translations.Queries;
using ATLAS.Kernel.i18n.Domain.Entities;
using ATLAS.Kernel.i18n.Domain.Exceptions;
using ATLAS.Kernel.i18n.Domain.Repositories;
using ATLAS.Kernel.i18n.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace ATLAS.Kernel.i18n.Application.Tests.Translations;

public sealed class GetTranslationQueryHandlerTests
{
    private readonly Mock<ITranslationRepository> _repoMock;
    private readonly Mock<ICacheService>          _cacheMock;
    private readonly GetTranslationQueryHandler   _handler;

    public GetTranslationQueryHandlerTests()
    {
        _repoMock  = new Mock<ITranslationRepository>();
        _cacheMock = new Mock<ICacheService>();

        // Default: cache always misses
        _cacheMock
            .Setup(c => c.GetAsync<TranslationLookupDto>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationLookupDto?)null);

        _handler = new GetTranslationQueryHandler(_repoMock.Object, _cacheMock.Object);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExactLanguageFound_ReturnsDtoWithFallbackFalse()
    {
        var translation = Translation.Create("CRM0001", "ES", "Cliente");

        _repoMock
            .Setup(r => r.FindAsync(
                It.Is<TranslationCode>(c => c.Value == "CRM0001"),
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(translation);

        var result = await _handler.Handle(
            new GetTranslationQuery("CRM0001", "ES"),
            CancellationToken.None);

        result.Code.Should().Be("CRM0001");
        result.LanguageCode.Should().Be("ES");
        result.Text.Should().Be("Cliente");
        result.IsFallback.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RequestedLanguageMissing_FallsBackToEnglish()
    {
        var englishTranslation = Translation.Create("CRM0001", "EN", "Customer");

        // ES not found
        _repoMock
            .Setup(r => r.FindAsync(
                It.Is<TranslationCode>(c => c.Value == "CRM0001"),
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);

        // EN found
        _repoMock
            .Setup(r => r.FindAsync(
                It.Is<TranslationCode>(c => c.Value == "CRM0001"),
                It.Is<LanguageCode>(l => l.Value == "EN"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(englishTranslation);

        var result = await _handler.Handle(
            new GetTranslationQuery("CRM0001", "ES"),
            CancellationToken.None);

        result.Text.Should().Be("Customer");
        result.IsFallback.Should().BeTrue("EN was used as fallback");
    }

    [Fact]
    public async Task Handle_NeitherLanguageNorFallbackFound_ThrowsTranslationNotFoundException()
    {
        _repoMock
            .Setup(r => r.FindAsync(
                It.IsAny<TranslationCode>(),
                It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Translation?)null);

        var act = async () => await _handler.Handle(
            new GetTranslationQuery("CRM0001", "ES"),
            CancellationToken.None);

        await act.Should().ThrowAsync<TranslationNotFoundException>();
    }

    // ─── Cache behaviour ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CacheHit_ReturnsFromCacheWithoutHittingRepo()
    {
        var cached = new TranslationLookupDto("CRM0001", "ES", "Cliente (cached)", false);

        _cacheMock
            .Setup(c => c.GetAsync<TranslationLookupDto>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _handler.Handle(
            new GetTranslationQuery("CRM0001", "ES"),
            CancellationToken.None);

        result.Text.Should().Be("Cliente (cached)");

        // Repository should never have been called
        _repoMock.Verify(
            r => r.FindAsync(
                It.IsAny<TranslationCode>(),
                It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_WritesResultToCache()
    {
        var translation = Translation.Create("CRM0001", "ES", "Cliente");

        _repoMock
            .Setup(r => r.FindAsync(
                It.IsAny<TranslationCode>(),
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(translation);

        await _handler.Handle(
            new GetTranslationQuery("CRM0001", "ES"),
            CancellationToken.None);

        _cacheMock.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<TranslationLookupDto>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("",       "ES")]   // empty code
    [InlineData("CRM001", "ES")]   // code too short (only 3 digits)
    [InlineData("CRM0001", "")]    // empty language
    [InlineData("CRM0001", "ESP")] // language code too long
    public void Validator_InvalidInput_HasErrors(string code, string language)
    {
        var validator = new GetTranslationQueryValidator();
        var result    = validator.Validate(new GetTranslationQuery(code, language));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ValidInput_HasNoErrors()
    {
        var validator = new GetTranslationQueryValidator();
        var result    = validator.Validate(new GetTranslationQuery("CRM0001", "ES"));

        result.IsValid.Should().BeTrue();
    }
}
