using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;
using FluentAssertions;

namespace ATLAS.Kernel.i18n.Application.UnitTests.Features.Translations.Validators;

public sealed class GetTranslationQueryValidatorTests
{
    private readonly GetTranslationQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetTranslationQuery("CRM0001", "es-ES"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyValues_Fails()
    {
        var result = _validator.Validate(new GetTranslationQuery("", ""));
        result.IsValid.Should().BeFalse();
    }
}

