using ATLAS.Kernel.i18n.Domain.Entities;
using ATLAS.Kernel.i18n.Domain.Exceptions;
using ATLAS.Kernel.i18n.Domain.ValueObjects;
using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;
using FluentAssertions;
using Xunit;

namespace ATLAS.Kernel.i18n.Domain.Tests.Entities;

public sealed class CurrencyFormatTests
{
    // ─── Factory ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ProducesFormat()
    {
        var fmt = CurrencyFormat.Create(
            "EN", "USD", "US Dollar", "$",
            CurrencySymbolPosition.Before,
            spaceBetweenSymbolAndAmount: false,
            decimalSeparator:   '.',
            thousandsSeparator: ',',
            decimalPlaces:      2);

        fmt.CurrencyCode.Should().Be("USD");
        fmt.Symbol.Should().Be("$");
        fmt.SymbolPosition.Should().Be(CurrencySymbolPosition.Before);
        fmt.DecimalPlaces.Should().Be(2);
    }

    [Fact]
    public void Create_CurrencyCodeNormalized_ToUpperCase()
    {
        var fmt = CurrencyFormat.Create(
            "EN", "usd", "US Dollar", "$",
            CurrencySymbolPosition.Before, false, '.', ',', 2);

        fmt.CurrencyCode.Should().Be("USD");
    }

    [Theory]
    [InlineData("EU")]    // too short
    [InlineData("EURO")]  // too long
    [InlineData("EU1")]   // contains digit
    public void Create_InvalidCurrencyCode_ThrowsDomainException(string currencyCode)
    {
        var act = () => CurrencyFormat.Create(
            "EN", currencyCode, "Euro", "€",
            CurrencySymbolPosition.Before, false, '.', ',', 2);

        act.Should().Throw<I18nDomainException>()
            .WithMessage("*CurrencyCode*");
    }

    [Fact]
    public void Create_SameDecimalAndThousandsSeparator_ThrowsDomainException()
    {
        var act = () => CurrencyFormat.Create(
            "EN", "USD", "US Dollar", "$",
            CurrencySymbolPosition.Before, false,
            decimalSeparator:   ',',
            thousandsSeparator: ',',   // same as decimal!
            decimalPlaces:      2);

        act.Should().Throw<I18nDomainException>()
            .WithMessage("*DecimalSeparator*ThousandsSeparator*different*");
    }

    [Fact]
    public void Create_NegativePatternWithoutAmountToken_ThrowsDomainException()
    {
        var act = () => CurrencyFormat.Create(
            "EN", "USD", "US Dollar", "$",
            CurrencySymbolPosition.Before, false, '.', ',', 2,
            negativePattern: "-{symbol}");  // missing {amount}

        act.Should().Throw<I18nDomainException>()
            .WithMessage("*{amount}*");
    }

    // ─── Format(decimal) ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(1234.56,   "$1,234.56")]
    [InlineData(0.00,      "$0.00")]
    [InlineData(1000000.0, "$1,000,000.00")]
    [InlineData(0.01,      "$0.01")]
    public void Format_EnglishUSD_ProducesExpectedString(decimal amount, string expected)
    {
        var fmt = CurrencyFormat.Create(
            "EN", "USD", "US Dollar", "$",
            CurrencySymbolPosition.Before, false, '.', ',', 2);

        fmt.Format(amount).Should().Be(expected);
    }

    [Theory]
    [InlineData(1234.56,   "1.234,56 €")]
    [InlineData(0.00,      "0,00 €")]
    [InlineData(1000000.0, "1.000.000,00 €")]
    public void Format_SpanishEUR_ProducesExpectedString(decimal amount, string expected)
    {
        var fmt = CurrencyFormat.Create(
            "ES", "EUR", "Euro", "€",
            CurrencySymbolPosition.After,
            spaceBetweenSymbolAndAmount: true,
            decimalSeparator:   ',',
            thousandsSeparator: '.',
            decimalPlaces:      2);

        fmt.Format(amount).Should().Be(expected);
    }

    [Fact]
    public void Format_JapaneseYen_ZeroDecimalPlaces()
    {
        var fmt = CurrencyFormat.Create(
            "EN", "JPY", "Japanese Yen", "¥",
            CurrencySymbolPosition.Before, false, '.', ',', 0);

        fmt.Format(1234.56m).Should().Be("¥1,235"); // rounds to nearest integer
    }

    [Fact]
    public void Format_NegativeAmount_UsesNegativePattern()
    {
        var fmt = CurrencyFormat.Create(
            "EN", "USD", "US Dollar", "$",
            CurrencySymbolPosition.Before, false, '.', ',', 2,
            negativePattern: "({symbol}{amount})");

        fmt.Format(-1234.56m).Should().Be("($1,234.56)");
    }
}

public sealed class LocaleConfigurationTests
{
    [Fact]
    public void Create_ValidData_ProducesConfiguration()
    {
        var config = LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, ',', '.', 2, 2);

        config.LanguageCode.Value.Should().Be("ES");
        config.DateFormat.Should().Be("dd/MM/yyyy");
        config.DecimalSeparator.Should().Be(',');
        config.ThousandsSeparator.Should().Be('.');
        config.FirstDayOfWeek.Should().Be(FirstDayOfWeek.Monday);
    }

    [Fact]
    public void Create_SameSeparators_ThrowsDomainException()
    {
        var act = () => LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, '.', '.', 2, 2);

        act.Should().Throw<I18nDomainException>()
            .WithMessage("*DecimalSeparator*ThousandsSeparator*different*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_DecimalPlacesOutOfRange_ThrowsDomainException(int decimals)
    {
        var act = () => LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, ',', '.', decimals, 2);

        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void Update_ChangesSeparators_ValidatesNewValues()
    {
        var config = LocaleConfiguration.Create(
            "ES", "dd/MM/yyyy", "d/M/yy", "HH:mm", "dd/MM/yyyy HH:mm",
            FirstDayOfWeek.Monday, ',', '.', 2, 2);

        config.Update("MM/dd/yyyy", "M/d/yy", "h:mm tt", "MM/dd/yyyy h:mm tt",
            FirstDayOfWeek.Sunday, '.', ',', 2, 2);

        config.DecimalSeparator.Should().Be('.');
        config.ThousandsSeparator.Should().Be(',');
        config.FirstDayOfWeek.Should().Be(FirstDayOfWeek.Sunday);
    }
}
