using KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;
using FluentAssertions;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Language.Validators;

public sealed class CreateLanguageCommandValidatorTests
{
    private readonly CreateLanguageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateLanguageCommand("es-ES", "Spanish", "Espanol"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidCode_Fails()
    {
        var result = _validator.Validate(new CreateLanguageCommand("", "Spanish", "Espanol"));
        result.IsValid.Should().BeFalse();
    }
}
