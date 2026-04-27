using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.ValueObjects;
using FluentAssertions;

namespace ATLAS.i18n.Domain.Tests.ValueObjects;

// ─── LanguageCode ─────────────────────────────────────────────────────────────

public sealed class LanguageCodeTests
{
    [Theory]
    [InlineData("EN")]
    [InlineData("ES")]
    [InlineData("en")]   // lowercase — should be normalized
    [InlineData("fr")]
    [InlineData("CA")]
    public void From_ValidCode_ReturnsNormalizedUpperCase(string input)
    {
        var code = LanguageCode.From(input);
        code.Value.Should().Be(input.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void From_EmptyOrNull_ThrowsDomainException(string? input)
    {
        var act = () => LanguageCode.From(input!);
        act.Should().Throw<I18nDomainException>()
            .WithMessage("Language code cannot be empty.");
    }

    [Theory]
    [InlineData("E")]       // too short
    [InlineData("ENG")]     // too long
    [InlineData("E1")]      // contains digit
    [InlineData("E-")]      // contains symbol
    public void From_InvalidFormat_ThrowsDomainException(string input)
    {
        var act = () => LanguageCode.From(input);
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = LanguageCode.From("ES");
        var b = LanguageCode.From("ES");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = LanguageCode.From("ES");
        var b = LanguageCode.From("EN");
        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string code = LanguageCode.From("DE");
        code.Should().Be("DE");
    }

    [Fact]
    public void WellKnownConstants_HaveCorrectValues()
    {
        LanguageCode.English.Value.Should().Be("EN");
        LanguageCode.Spanish.Value.Should().Be("ES");
        LanguageCode.Catalan.Value.Should().Be("CA");
        LanguageCode.French.Value.Should().Be("FR");
    }
}

// ─── TranslationCode ─────────────────────────────────────────────────────────

public sealed class TranslationCodeTests
{
    [Theory]
    [InlineData("CRM0001", "CRM", 1)]
    [InlineData("PIM0042", "PIM", 42)]
    [InlineData("AUTH9999", "AUTH", 9999)]
    [InlineData("WMS0100", "WMS",  100)]
    [InlineData("ATLAS0001", "ATLAS", 1)]   // 5-letter module
    [InlineData("crm0001",   "CRM",   1)]   // lowercase input — normalized
    public void From_ValidCode_ParsesCorrectly(
        string input, string expectedModule, int expectedSeq)
    {
        var code = TranslationCode.From(input);

        code.Module.Should().Be(expectedModule);
        code.Sequence.Should().Be(expectedSeq);
        code.Value.Should().Be($"{expectedModule}{expectedSeq:D4}");
    }

    [Fact]
    public void Create_ValidComponents_BuildsCode()
    {
        var code = TranslationCode.Create("CRM", 1);
        code.Value.Should().Be("CRM0001");
        code.Module.Should().Be("CRM");
        code.Sequence.Should().Be(1);
    }

    [Fact]
    public void Create_MaxSequence_Succeeds()
    {
        var code = TranslationCode.Create("PIM", 9999);
        code.Value.Should().Be("PIM9999");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void From_EmptyOrNull_ThrowsDomainException(string? input)
    {
        var act = () => TranslationCode.From(input!);
        act.Should().Throw<I18nDomainException>();
    }

    [Theory]
    [InlineData("0001")]         // no module prefix
    [InlineData("CRM001")]       // only 3 digits
    [InlineData("CRM00001")]     // 5 digits
    [InlineData("C0001")]        // module too short (1 char)
    [InlineData("TOOLONG0001")]  // module > 5 chars
    [InlineData("CRM000A")]      // non-digit at end
    [InlineData("1RM0001")]      // module starts with digit
    public void From_InvalidFormat_ThrowsDomainException(string input)
    {
        var act = () => TranslationCode.From(input);
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void Create_SequenceZero_ThrowsDomainException()
    {
        var act = () => TranslationCode.Create("CRM", 0);
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void Create_SequenceOver9999_ThrowsDomainException()
    {
        var act = () => TranslationCode.Create("CRM", 10000);
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void Equality_SameCode_AreEqual()
    {
        var a = TranslationCode.From("CRM0001");
        var b = TranslationCode.Create("CRM", 1);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentCode_AreNotEqual()
    {
        var a = TranslationCode.From("CRM0001");
        var b = TranslationCode.From("CRM0002");
        a.Should().NotBe(b);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string code = TranslationCode.From("PIM0001");
        code.Should().Be("PIM0001");
    }
}
