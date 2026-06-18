using KUKULCAN.Kernel.Domain.Result;
using KUKULCAN.Kernel.i18n.Domain.Entities;
using FluentAssertions;

namespace KUKULCAN.Kernel.i18n.Domain.UnitTests;

public sealed class TranslationTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsSuccess()
    {
        Result<Translation> result = Translation.Create(
            Guid.NewGuid(),
            "CRM0001",
            "es-ES",
            "Cliente"
        );
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be("CRM0001");
        result.Value.IsReviewed.Should().BeFalse();
    }

    [Fact]
    public void UpdateText_TooLongForMaxLength_ReturnsFailure()
    {
        Result<Translation> result = Translation.Create(
            Guid.NewGuid(),
            "CRM0001",
            "es-ES",
            "12345",
            maxLength:
            5
        );
        result.IsSuccess.Should().BeTrue();

        Result update = result.Value.UpdateText("123456");
        update.IsFailure.Should().BeTrue();
    }
}

