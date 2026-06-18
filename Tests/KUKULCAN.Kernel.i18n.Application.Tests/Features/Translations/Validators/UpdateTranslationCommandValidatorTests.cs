using KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;
using FluentAssertions;

namespace KUKULCAN.Kernel.i18n.Application.UnitTests.Features.Translations.Validators;

public sealed class UpdateTranslationCommandValidatorTests
{
    private readonly UpdateTranslationCommandValidator _v = new();

    [Fact]
    public void Validate_Valid_Passes() =>
        _v
            .Validate(
                new UpdateTranslationCommand(
                    "CRM0001",
                    "es-ES",
                    "txt"
                )
            )
            .IsValid
            .Should()
            .BeTrue();
    [Fact]
    public void Validate_EmptyText_Fails() =>
        _v
            .Validate(
                new UpdateTranslationCommand(
                    "CRM0001",
                    "es-ES",
                    ""
                )
            )
            .IsValid
            .Should()
            .BeFalse();
}

