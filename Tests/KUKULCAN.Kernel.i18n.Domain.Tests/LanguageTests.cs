using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Domain.Entities;
using FluentAssertions;

namespace KUKULCAN.Kernel.i18n.Domain.UnitTests;

public sealed class LanguageTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsSuccess()
    {
        Result<Language> result = Language.Create(
            Guid.NewGuid(),
            "es-ES",
            "Spanish",
            "Espanol"
        );
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("es-ES");
        result.Value.Name.Should().Be("Spanish");
    }

    [Fact]
    public void Deactivate_DefaultLanguage_ReturnsFailure()
    {
        Result<Language> created = Language.Create(
            Guid.NewGuid(),
            "en-US",
            "English",
            "English",
            isDefault: true
        );
        created.IsSuccess.Should().BeTrue();
        Result result = created.Value.Deactivate();
        result.IsFailure.Should().BeTrue();
    }
}

