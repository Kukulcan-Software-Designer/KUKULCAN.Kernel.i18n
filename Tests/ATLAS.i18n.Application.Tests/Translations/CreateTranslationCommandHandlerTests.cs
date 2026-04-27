using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Application.Translations.Commands;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace ATLAS.i18n.Application.Tests.Translations;

public sealed class CreateTranslationCommandHandlerTests
{
    private readonly Mock<ITranslationRepository> _translationRepo;
    private readonly Mock<ILanguageRepository>    _languageRepo;
    private readonly Mock<IUnitOfWork>            _unitOfWork;
    private readonly Mock<ICacheService>          _cache;
    private readonly CreateTranslationCommandHandler _handler;

    public CreateTranslationCommandHandlerTests()
    {
        _translationRepo = new Mock<ITranslationRepository>();
        _languageRepo    = new Mock<ILanguageRepository>();
        _unitOfWork      = new Mock<IUnitOfWork>();
        _cache           = new Mock<ICacheService>();

        _handler = new CreateTranslationCommandHandler(
            _translationRepo.Object,
            _languageRepo.Object,
            _unitOfWork.Object,
            _cache.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTranslation()
    {
        var activeLanguage = Language.Create("ES", "Spanish", "Español", "es-ES");

        _languageRepo
            .Setup(r => r.GetByCodeAsync(
                It.Is<LanguageCode>(l => l.Value == "ES"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeLanguage);

        _translationRepo
            .Setup(r => r.ExistsAsync(
                It.IsAny<TranslationCode>(),
                It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateTranslationCommand("CRM0001", "ES", "Cliente");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Code.Should().Be("CRM0001");
        result.LanguageCode.Should().Be("ES");
        result.Text.Should().Be("Cliente");
        result.IsReviewed.Should().BeFalse();

        _translationRepo.Verify(
            r => r.Add(It.Is<Translation>(t =>
                t.Code.Value == "CRM0001" && t.Text == "Cliente")),
            Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LanguageNotFound_ThrowsLanguageNotFoundException()
    {
        _languageRepo
            .Setup(r => r.GetByCodeAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Language?)null);

        var act = async () => await _handler.Handle(
            new CreateTranslationCommand("CRM0001", "XX", "Text"),
            CancellationToken.None);

        await act.Should().ThrowAsync<LanguageNotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveLanguage_ThrowsDomainException()
    {
        var inactive = Language.Create("FR", "French", "Français", "fr-FR");
        inactive.Deactivate();

        _languageRepo
            .Setup(r => r.GetByCodeAsync(
                It.Is<LanguageCode>(l => l.Value == "FR"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var act = async () => await _handler.Handle(
            new CreateTranslationCommand("CRM0001", "FR", "Client"),
            CancellationToken.None);

        await act.Should().ThrowAsync<I18nDomainException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public async Task Handle_DuplicateTranslation_ThrowsDuplicateTranslationException()
    {
        var active = Language.Create("ES", "Spanish", "Español", "es-ES");

        _languageRepo
            .Setup(r => r.GetByCodeAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        _translationRepo
            .Setup(r => r.ExistsAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);  // already exists!

        var act = async () => await _handler.Handle(
            new CreateTranslationCommand("CRM0001", "ES", "Cliente"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateTranslationException>();
    }

    [Fact]
    public async Task Handle_OnSuccess_InvalidatesCache()
    {
        var active = Language.Create("ES", "Spanish", "Español", "es-ES");

        _languageRepo
            .Setup(r => r.GetByCodeAsync(It.IsAny<LanguageCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        _translationRepo
            .Setup(r => r.ExistsAsync(It.IsAny<TranslationCode>(), It.IsAny<LanguageCode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.Handle(
            new CreateTranslationCommand("CRM0001", "ES", "Cliente"),
            CancellationToken.None);

        _cache.Verify(
            c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2),
            "Should invalidate both the single-key cache and the module cache");
    }
}

public sealed class BulkUpsertTranslationsCommandValidatorTests
{
    private readonly BulkUpsertTranslationsCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyItems_HasError()
    {
        var cmd    = new BulkUpsertTranslationsCommand([]);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_Over5000Items_HasError()
    {
        var items = Enumerable.Range(1, 5001)
            .Select(i => new BulkTranslationItem($"CRM{i:D4}", "ES", $"Text {i}"))
            .ToList();

        var cmd    = new BulkUpsertTranslationsCommand(items);
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidItems_NoErrors()
    {
        var cmd = new BulkUpsertTranslationsCommand(
        [
            new BulkTranslationItem("CRM0001", "ES", "Cliente"),
            new BulkTranslationItem("CRM0002", "ES", "Pedido"),
        ]);

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ItemWithEmptyText_HasError()
    {
        var cmd = new BulkUpsertTranslationsCommand(
        [
            new BulkTranslationItem("CRM0001", "ES", ""),
        ]);

        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
