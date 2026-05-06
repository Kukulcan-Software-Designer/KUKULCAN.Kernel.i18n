using ATLAS.Kernel.i18n.Domain.Entities;
using ATLAS.Kernel.i18n.Domain.Exceptions;
using ATLAS.Kernel.i18n.Domain.ValueObjects;
using FluentAssertions;

namespace ATLAS.Kernel.i18n.Domain.Tests.Entities;

public sealed class LanguageTests
{
    // ─── Factory ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ProducesActiveLanguage()
    {
        var lang = Language.Create("ES", "Spanish", "Español", "es-ES");

        lang.Id.Value.Should().Be("ES");
        lang.Name.Should().Be("Spanish");
        lang.NativeName.Should().Be("Español");
        lang.CultureTag.Should().Be("es-ES");
        lang.IsActive.Should().BeTrue();
        lang.IsDefault.Should().BeFalse();
        lang.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_AsDefault_SetsIsDefaultTrue()
    {
        var lang = Language.Create("EN", "English", "English", "en-US", isDefault: true);
        lang.IsDefault.Should().BeTrue();
        lang.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Spanish", "Español", "es-ES")]
    [InlineData("ES", "", "Español", "es-ES")]
    [InlineData("ES", "Spanish", "", "es-ES")]
    [InlineData("ES", "Spanish", "Español", "")]
    public void Create_MissingRequiredFields_ThrowsArgumentException(
        string code, string name, string nativeName, string cultureTag)
    {
        var act = () => Language.Create(code, name, nativeName, cultureTag);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        var lang = Language.Create("FR", "French", "Français", "fr-FR");

        lang.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LanguageCreatedEvent>()
            .Which.Code.Should().Be("FR");
    }

    // ─── Activate / Deactivate ────────────────────────────────────────────────

    [Fact]
    public void Deactivate_NonDefaultLanguage_Succeeds()
    {
        var lang = Language.Create("FR", "French", "Français", "fr-FR");
        lang.Deactivate();
        lang.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_DefaultLanguage_ThrowsDomainException()
    {
        var lang = Language.Create("EN", "English", "English", "en-US", isDefault: true);

        var act = () => lang.Deactivate();

        act.Should().Throw<DefaultLanguageCannotBeDeactivatedException>()
            .WithMessage("*EN*");
    }

    [Fact]
    public void Activate_InactiveLanguage_BecomesActive()
    {
        var lang = Language.Create("DE", "German", "Deutsch", "de-DE");
        lang.Deactivate();
        lang.IsActive.Should().BeFalse();

        lang.Activate();
        lang.IsActive.Should().BeTrue();
    }

    // ─── Locale Configuration ─────────────────────────────────────────────────

    [Fact]
    public void SetLocaleConfiguration_ValidConfig_AttachesConfig()
    {
        var lang   = Language.Create("ES", "Spanish", "Español", "es-ES");
        var config = LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, ',', '.', 2, 2);

        lang.SetLocaleConfiguration(config);

        lang.LocaleConfiguration.Should().NotBeNull();
        lang.LocaleConfiguration!.DecimalSeparator.Should().Be(',');
    }

    [Fact]
    public void SetLocaleConfiguration_Null_ThrowsArgumentNullException()
    {
        var lang = Language.Create("ES", "Spanish", "Español", "es-ES");
        var act  = () => lang.SetLocaleConfiguration(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ─── Currency Formats ─────────────────────────────────────────────────────

    [Fact]
    public void AddCurrencyFormat_NewCurrency_AddsSuccessfully()
    {
        var lang   = Language.Create("ES", "Spanish", "Español", "es-ES");
        var format = CurrencyFormat.Create(
            "ES", "EUR", "Euro", "€",
            CurrencySymbolPosition.After, true, ',', '.', 2);

        lang.AddCurrencyFormat(format);

        lang.CurrencyFormats.Should().HaveCount(1);
    }

    [Fact]
    public void AddCurrencyFormat_DuplicateCurrency_ThrowsDomainException()
    {
        var lang = Language.Create("ES", "Spanish", "Español", "es-ES");
        var eur1 = CurrencyFormat.Create("ES", "EUR", "Euro", "€",
            CurrencySymbolPosition.After, true, ',', '.', 2);
        var eur2 = CurrencyFormat.Create("ES", "EUR", "Euro", "€",
            CurrencySymbolPosition.After, true, ',', '.', 2);

        lang.AddCurrencyFormat(eur1);

        var act = () => lang.AddCurrencyFormat(eur2);
        act.Should().Throw<I18nDomainException>()
            .WithMessage("*EUR*already exists*");
    }

    [Fact]
    public void RemoveCurrencyFormat_ExistingCurrency_RemovesIt()
    {
        var lang = Language.Create("ES", "Spanish", "Español", "es-ES");
        var fmt  = CurrencyFormat.Create("ES", "EUR", "Euro", "€",
            CurrencySymbolPosition.After, true, ',', '.', 2);

        lang.AddCurrencyFormat(fmt);
        lang.RemoveCurrencyFormat("EUR");

        lang.CurrencyFormats.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCurrencyFormat_NonExistent_ThrowsDomainException()
    {
        var lang = Language.Create("ES", "Spanish", "Español", "es-ES");
        var act  = () => lang.RemoveCurrencyFormat("USD");
        act.Should().Throw<I18nDomainException>();
    }
}
