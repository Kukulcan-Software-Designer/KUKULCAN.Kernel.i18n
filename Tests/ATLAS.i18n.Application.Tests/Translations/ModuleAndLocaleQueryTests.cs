using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Application.Translations.Queries;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace ATLAS.i18n.Application.Tests.Translations;

public sealed class GetTranslationsByModuleQueryHandlerTests
{
    private readonly Mock<ITranslationRepository> _repoMock;
    private readonly Mock<ICacheService>          _cacheMock;
    private readonly GetTranslationsByModuleQueryHandler _handler;

    public GetTranslationsByModuleQueryHandlerTests()
    {
        _repoMock  = new Mock<ITranslationRepository>();
        _cacheMock = new Mock<ICacheService>();

        _cacheMock
            .Setup(c => c.GetAsync<TranslationMapDto>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationMapDto?)null);

        _handler = new GetTranslationsByModuleQueryHandler(_repoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_AllTranslationsExistInRequestedLanguage_ReturnsFullDictionary()
    {
        var crm0001Es = Translation.Create("CRM0001", "ES", "Cliente");
        var crm0002Es = Translation.Create("CRM0002", "ES", "Pedido");

        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync("CRM",
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([crm0001Es, crm0002Es]);

        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync("CRM",
                It.Is<LanguageCode>(l => l.Value == "EN"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(
            new GetTranslationsByModuleQuery("CRM", "ES"),
            CancellationToken.None);

        result.Module.Should().Be("CRM");
        result.LanguageCode.Should().Be("ES");
        result.Translations.Should().HaveCount(2);
        result.Translations["CRM0001"].Should().Be("Cliente");
        result.Translations["CRM0002"].Should().Be("Pedido");
    }

    [Fact]
    public async Task Handle_PartialSpanishTranslations_FillsGapsWithEnglish()
    {
        // CRM0001 only in ES; CRM0002 only in EN — merge should fill the gap
        var crm0001Es = Translation.Create("CRM0001", "ES", "Cliente");
        var crm0002En = Translation.Create("CRM0002", "EN", "Order");

        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync("CRM",
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([crm0001Es]);

        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync("CRM",
                It.Is<LanguageCode>(l => l.Value == "EN"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([crm0002En]);

        var result = await _handler.Handle(
            new GetTranslationsByModuleQuery("CRM", "ES"),
            CancellationToken.None);

        result.Translations.Should().HaveCount(2);
        result.Translations["CRM0001"].Should().Be("Cliente",  "ES translation wins");
        result.Translations["CRM0002"].Should().Be("Order", "EN fills the gap");
    }

    [Fact]
    public async Task Handle_EnglishRequested_DoesNotQueryEnglishTwice()
    {
        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync("CRM",
                It.Is<LanguageCode>(l => l.Value == "EN"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Translation.Create("CRM0001", "EN", "Customer")]);

        await _handler.Handle(
            new GetTranslationsByModuleQuery("CRM", "EN"),
            CancellationToken.None);

        // English should be fetched exactly once when the requested language IS English
        _repoMock.Verify(
            r => r.GetByModuleAndLanguageAsync(
                "CRM",
                It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ModuleNormalized_ToUpperCase()
    {
        _repoMock
            .Setup(r => r.GetByModuleAndLanguageAsync(
                It.IsAny<string>(), It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(
            new GetTranslationsByModuleQuery("crm", "ES"),  // lowercase module
            CancellationToken.None);

        result.Module.Should().Be("CRM");

        _repoMock.Verify(
            r => r.GetByModuleAndLanguageAsync(
                "CRM",   // must be normalized
                It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}

// ─── Locale configuration query tests ────────────────────────────────────────

public sealed class GetLocaleConfigurationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingConfig_ReturnsDto()
    {
        var config = LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, ',', '.', 2, 2);

        var repoMock  = new Mock<Application.Locales.Queries.ILocaleConfigurationRepositoryForTest>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.GetAsync<Application.Common.DTOs.LocaleConfigurationDto>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Common.DTOs.LocaleConfigurationDto?)null);

        // Verify DTO mapping via the public handler method
        var dto = Application.Locales.Queries.GetLocaleConfigurationQueryHandler.MapToDto(config);

        dto.LanguageCode.Should().Be("ES");
        dto.DateFormat.Should().Be("dd/MM/yyyy");
        dto.DecimalSeparator.Should().Be(",");
        dto.ThousandsSeparator.Should().Be(".");
        dto.FirstDayOfWeek.Should().Be("Monday");
        dto.DecimalPlaces.Should().Be(2);
        dto.CurrencyDecimalPlaces.Should().Be(2);
    }
}

// Marker interface to avoid a circular reference in the test assembly
namespace ATLAS.i18n.Application.Locales.Queries
{
    public interface ILocaleConfigurationRepositoryForTest { }
}
