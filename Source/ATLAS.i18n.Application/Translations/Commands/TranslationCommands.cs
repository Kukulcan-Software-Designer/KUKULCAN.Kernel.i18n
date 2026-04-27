using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ATLAS.i18n.Application.Translations.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// CREATE TRANSLATION
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Creates a new translation entry for a code + language combination.</summary>
public record CreateTranslationCommand(
    string Code,
    string LanguageCode,
    string Text,
    string? Context   = null,
    int?   MaxLength  = null)
    : IRequest<TranslationDto>;

public sealed class CreateTranslationCommandValidator
    : AbstractValidator<CreateTranslationCommand>
{
    public CreateTranslationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(c =>
            {
                try { TranslationCode.From(c); return true; }
                catch { return false; }
            })
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().Length(2).Matches("^[a-zA-Z]{2}$");

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(4000);

        When(x => x.MaxLength.HasValue, () =>
            RuleFor(x => x.MaxLength!.Value).GreaterThan(0));

        When(x => x.MaxLength.HasValue && !string.IsNullOrEmpty(x.Text), () =>
            RuleFor(x => x)
                .Must(x => x.Text.Length <= x.MaxLength!.Value)
                .WithMessage("Text exceeds the specified MaxLength."));
    }
}

public sealed class CreateTranslationCommandHandler
    : IRequestHandler<CreateTranslationCommand, TranslationDto>
{
    private readonly ITranslationRepository _repository;
    private readonly ILanguageRepository    _languageRepo;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public CreateTranslationCommandHandler(
        ITranslationRepository repository,
        ILanguageRepository languageRepo,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<TranslationDto> Handle(
        CreateTranslationCommand request,
        CancellationToken cancellationToken)
    {
        var code = TranslationCode.From(request.Code);
        var lang = LanguageCode.From(request.LanguageCode);

        // Verify language exists and is active
        var language = await _languageRepo.GetByCodeAsync(lang, cancellationToken)
            ?? throw new LanguageNotFoundException(lang.Value);

        if (!language.IsActive)
            throw new I18nDomainException(
                $"Language '{lang.Value}' is inactive. Translations cannot be added to inactive languages.");

        // Enforce uniqueness
        if (await _repository.ExistsAsync(code, lang, cancellationToken))
            throw new DuplicateTranslationException(code.Value, lang.Value);

        var translation = Translation.Create(
            request.Code,
            request.LanguageCode,
            request.Text,
            request.Context,
            request.MaxLength);

        _repository.Add(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate relevant cache entries
        await _cache.RemoveAsync(CacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.TranslationsModule(code.Module, lang.Value), cancellationToken);

        return MapToDto(translation);
    }

    private static TranslationDto MapToDto(Domain.Entities.Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed, t.CreatedAt, t.UpdatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════════
// UPDATE TRANSLATION TEXT
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Updates the text of an existing translation. Resets review status.</summary>
public record UpdateTranslationCommand(
    string Code,
    string LanguageCode,
    string NewText,
    string? NewContext = null)
    : IRequest<TranslationDto>;

public sealed class UpdateTranslationCommandValidator
    : AbstractValidator<UpdateTranslationCommand>
{
    public UpdateTranslationCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.LanguageCode).NotEmpty().Length(2).Matches("^[a-zA-Z]{2}$");
        RuleFor(x => x.NewText).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateTranslationCommandHandler
    : IRequestHandler<UpdateTranslationCommand, TranslationDto>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public UpdateTranslationCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<TranslationDto> Handle(
        UpdateTranslationCommand request,
        CancellationToken cancellationToken)
    {
        var code        = TranslationCode.From(request.Code);
        var lang        = LanguageCode.From(request.LanguageCode);
        var translation = await _repository.FindAsync(code, lang, cancellationToken)
            ?? throw new TranslationNotFoundException(code.Value, lang.Value);

        translation.UpdateText(request.NewText);
        translation.UpdateContext(request.NewContext);

        _repository.Update(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.TranslationsModule(code.Module, lang.Value), cancellationToken);

        return new TranslationDto(
            translation.Id, translation.Code.Value, translation.Code.Module,
            translation.LanguageCode.Value, translation.Text, translation.Context,
            translation.MaxLength, translation.IsReviewed,
            translation.CreatedAt, translation.UpdatedAt);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DELETE TRANSLATION
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Deletes a translation for a specific code + language combination.</summary>
public record DeleteTranslationCommand(string Code, string LanguageCode)
    : IRequest<Unit>;

public sealed class DeleteTranslationCommandHandler
    : IRequestHandler<DeleteTranslationCommand, Unit>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public DeleteTranslationCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Unit> Handle(
        DeleteTranslationCommand request,
        CancellationToken cancellationToken)
    {
        var code        = TranslationCode.From(request.Code);
        var lang        = LanguageCode.From(request.LanguageCode);
        var translation = await _repository.FindAsync(code, lang, cancellationToken)
            ?? throw new TranslationNotFoundException(code.Value, lang.Value);

        // Protect default-language (EN) entries — warn but allow via explicit flag
        if (lang == LanguageCode.English)
            throw new I18nDomainException(
                $"Cannot delete the English (default) translation for '{code.Value}'. " +
                "Remove all other language variants first, or use bulk-delete.");

        _repository.Remove(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.TranslationsModule(code.Module, lang.Value), cancellationToken);

        return Unit.Value;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// MARK AS REVIEWED / UNREVIEWED
// ═══════════════════════════════════════════════════════════════════════════════

public record MarkTranslationReviewedCommand(string Code, string LanguageCode, bool IsReviewed)
    : IRequest<Unit>;

public sealed class MarkTranslationReviewedCommandHandler
    : IRequestHandler<MarkTranslationReviewedCommand, Unit>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public MarkTranslationReviewedCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        MarkTranslationReviewedCommand request,
        CancellationToken cancellationToken)
    {
        var code        = TranslationCode.From(request.Code);
        var lang        = LanguageCode.From(request.LanguageCode);
        var translation = await _repository.FindAsync(code, lang, cancellationToken)
            ?? throw new TranslationNotFoundException(code.Value, lang.Value);

        if (request.IsReviewed)
            translation.MarkAsReviewed();
        else
            translation.MarkAsUnreviewed();

        _repository.Update(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// BULK UPSERT (import)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Inserts or updates a batch of translations in a single operation.
/// Used during import, initial seeding, or CI/CD pipeline updates.
/// </summary>
public record BulkUpsertTranslationsCommand(
    IReadOnlyList<BulkTranslationItem> Items)
    : IRequest<BulkUpsertResult>;

public record BulkTranslationItem(
    string Code,
    string LanguageCode,
    string Text,
    string? Context  = null,
    int?   MaxLength = null);

public record BulkUpsertResult(
    int Created,
    int Updated,
    IReadOnlyList<string> Errors);

public sealed class BulkUpsertTranslationsCommandValidator
    : AbstractValidator<BulkUpsertTranslationsCommand>
{
    public BulkUpsertTranslationsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleFor(x => x.Items).Must(i => i.Count <= 5000)
            .WithMessage("Maximum 5,000 items per bulk operation.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Code).NotEmpty();
            item.RuleFor(i => i.LanguageCode).NotEmpty().Length(2);
            item.RuleFor(i => i.Text).NotEmpty().MaximumLength(4000);
        });
    }
}

public sealed class BulkUpsertTranslationsCommandHandler
    : IRequestHandler<BulkUpsertTranslationsCommand, BulkUpsertResult>
{
    private readonly ITranslationRepository _repository;
    private readonly ILanguageRepository    _languageRepo;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public BulkUpsertTranslationsCommandHandler(
        ITranslationRepository repository,
        ILanguageRepository languageRepo,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<BulkUpsertResult> Handle(
        BulkUpsertTranslationsCommand request,
        CancellationToken cancellationToken)
    {
        int created = 0, updated = 0;
        var errors          = new List<string>();
        var modulesToInvalidate = new HashSet<(string Module, string Lang)>();

        // Pre-load active languages for validation
        var activeLangs = (await _languageRepo.GetAllActiveAsync(cancellationToken))
            .Select(l => l.Id.Value)
            .ToHashSet();

        foreach (var item in request.Items)
        {
            try
            {
                var code = TranslationCode.From(item.Code);
                var lang = LanguageCode.From(item.LanguageCode);

                if (!activeLangs.Contains(lang.Value))
                {
                    errors.Add($"{item.Code}/{item.LanguageCode}: Language not found or inactive.");
                    continue;
                }

                var existing = await _repository.FindAsync(code, lang, cancellationToken);
                if (existing is null)
                {
                    var translation = Translation.Create(
                        item.Code, item.LanguageCode, item.Text, item.Context, item.MaxLength);
                    _repository.Add(translation);
                    created++;
                }
                else
                {
                    existing.UpdateText(item.Text);
                    existing.UpdateContext(item.Context);
                    _repository.Update(existing);
                    updated++;
                }

                modulesToInvalidate.Add((code.Module, lang.Value));
            }
            catch (Exception ex)
            {
                errors.Add($"{item.Code}/{item.LanguageCode}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache for affected modules
        foreach (var (module, lang) in modulesToInvalidate)
            await _cache.RemoveAsync(CacheKeys.TranslationsModule(module, lang), cancellationToken);

        return new BulkUpsertResult(created, updated, errors);
    }
}
