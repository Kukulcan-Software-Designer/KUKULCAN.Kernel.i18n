using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using FluentAssertions;

namespace ATLAS.i18n.Domain.Tests.Entities;

public sealed class TranslationTests
{
    // ─── Factory ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ProducesTranslation()
    {
        var t = Translation.Create("CRM0001", "ES", "Cliente");

        t.Code.Value.Should().Be("CRM0001");
        t.LanguageCode.Value.Should().Be("ES");
        t.Text.Should().Be("Cliente");
        t.IsReviewed.Should().BeFalse();
        t.Context.Should().BeNull();
        t.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithContextAndMaxLength_StoresValues()
    {
        var t = Translation.Create("CRM0001", "ES", "Cliente",
            context: "CRM customer label", maxLength: 20);

        t.Context.Should().Be("CRM customer label");
        t.MaxLength.Should().Be(20);
    }

    [Fact]
    public void Create_TextExceedsMaxLength_ThrowsDomainException()
    {
        var act = () => Translation.Create("CRM0001", "ES", "Texto muy largo aquí", maxLength: 5);
        act.Should().Throw<I18nDomainException>()
            .WithMessage("*MaxLength*");
    }

    [Fact]
    public void Create_EmptyText_ThrowsArgumentException()
    {
        var act = () => Translation.Create("CRM0001", "ES", "");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_RaisesDomainEvent()
    {
        var t = Translation.Create("PIM0001", "EN", "Product");

        t.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TranslationCreatedEvent>()
            .Which.Code.Should().Be("PIM0001");
    }

    // ─── UpdateText ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateText_ValidText_UpdatesAndResetsReviewStatus()
    {
        var t = Translation.Create("CRM0001", "ES", "Cliente");
        t.MarkAsReviewed();
        t.IsReviewed.Should().BeTrue();

        t.UpdateText("Clientes");

        t.Text.Should().Be("Clientes");
        t.IsReviewed.Should().BeFalse("any text change resets the review flag");
    }

    [Fact]
    public void UpdateText_ExceedsMaxLength_ThrowsDomainException()
    {
        var t = Translation.Create("CRM0001", "ES", "Ok", maxLength: 5);

        var act = () => t.UpdateText("Texto demasiado largo");
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void UpdateText_RaisesDomainEvent()
    {
        var t = Translation.Create("CRM0001", "ES", "Cliente");
        t.ClearDomainEvents();

        t.UpdateText("Clientes");

        t.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TranslationTextUpdatedEvent>();
    }

    // ─── Review management ────────────────────────────────────────────────────

    [Fact]
    public void MarkAsReviewed_SetsReviewedTrue()
    {
        var t = Translation.Create("CRM0001", "EN", "Customer");
        t.MarkAsReviewed();
        t.IsReviewed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsUnreviewed_SetsReviewedFalse()
    {
        var t = Translation.Create("CRM0001", "EN", "Customer");
        t.MarkAsReviewed();
        t.MarkAsUnreviewed();
        t.IsReviewed.Should().BeFalse();
    }

    // ─── MaxLength ────────────────────────────────────────────────────────────

    [Fact]
    public void SetMaxLength_LessThanCurrentText_ThrowsDomainException()
    {
        var t   = Translation.Create("CRM0001", "ES", "Hola mundo");

        var act = () => t.SetMaxLength(3);   // "Hola mundo" is 10 chars
        act.Should().Throw<I18nDomainException>()
            .WithMessage("*Current text length*exceeds*");
    }

    [Fact]
    public void SetMaxLength_Zero_ThrowsDomainException()
    {
        var t   = Translation.Create("CRM0001", "ES", "Hola");
        var act = () => t.SetMaxLength(0);
        act.Should().Throw<I18nDomainException>();
    }

    [Fact]
    public void SetMaxLength_Null_ClearsRestriction()
    {
        var t = Translation.Create("CRM0001", "ES", "Hola", maxLength: 10);
        t.SetMaxLength(null);
        t.MaxLength.Should().BeNull();
    }

    // ─── Context ──────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateContext_TrimsWhitespace()
    {
        var t = Translation.Create("CRM0001", "EN", "Customer");
        t.UpdateContext("   label for the CRM grid   ");
        t.Context.Should().Be("label for the CRM grid");
    }

    [Fact]
    public void UpdateContext_Null_ClearsContext()
    {
        var t = Translation.Create("CRM0001", "EN", "Customer", context: "some context");
        t.UpdateContext(null);
        t.Context.Should().BeNull();
    }
}
