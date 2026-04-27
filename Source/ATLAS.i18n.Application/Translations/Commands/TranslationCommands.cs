using Atlas.SharedKernel.Infrastructure.Primitives;
using FluentValidation;

namespace ATLAS.i18n.Application.Translations.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// CREATE TRANSLATION
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateTranslationCommand(
    string  Code,
    string  LanguageCode,
    string  Text,
    string? Context   = null,
    int?    MaxLength = null)
    : IRequest<Result<TranslationDto>>;

public sealed class CreateTranslationCommandValidator
    : AbstractValidator<CreateTranslationCommand>
{
    public CreateTranslationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(c => TranslationCode.From(c).IsSuccess)
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag (e.g. es-ES, en-US).");

        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);

        When(x => x.MaxLength.HasValue, () =>
        {
            RuleFor(x => x.MaxLength!.Value).GreaterThan(0);
            RuleFor(x => x)
                .Must(x => !x.MaxLength.HasValue || x.Text.Length <= x.MaxLength.Value)
                .WithMessage("Text length exceeds the specified MaxLength.");
        });
    }
}

public sealed class CreateTranslationCommandHandler
    : IRequestHandler<CreateTranslationCommand, Result<TranslationDto>>
{
    private readonly ITranslationRepository _translationRepo;
    private readonly ILanguageRepository    _languageRepo;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public CreateTranslationCommandHandler(
        ITranslationRepository translationRepo,
        ILanguageRepository    languageRepo,
        IUnitOfWork            unitOfWork,
        ICacheService          cache)
    {
        _translationRepo = translationRepo;
        _languageRepo    = languageRepo;
        _unitOfWork      = unitOfWork;
        _cache           = cache;
    }

    public async Task<Result<TranslationDto>> Handle(
        CreateTranslationCommand request,
        CancellationToken        cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;

        // Verify language exists and is active
        var language = await _languageRepo.GetByCodeAsync(lang.Value, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        if (!language.IsActive)
            return Error.Conflict(
                "Language.Inactive",
                $"Language '{lang.Value}' is inactive. Translations cannot be added to inactive languages.");

        // Enforce uniqueness (code + language)
        if (await _translationRepo.ExistsAsync(code, lang, cancellationToken))
            return Error.Conflict(
                "Translation.Duplicate",
                $"A translation for '{code.Value}' in language '{lang.Value}' already exists.");

        // Create entity using Result pattern — use SequentialGuid for PostgreSQL optimisation
        var createResult = Translation.Create(
            SequentialGuid.NewSequentialGuidAtEnd(),
            request.Code,
            request.LanguageCode,
            request.Text,
            request.Context,
            request.MaxLength);

        if (createResult.IsFailure) return createResult.Error;

        await _translationRepo.AddAsync(createResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync(I18nCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return MapToDto(createResult.Value);
    }

    internal static TranslationDto MapToDto(Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed,
            t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy);
}

// ═══════════════════════════════════════════════════════════════════════════════
// UPDATE TRANSLATION TEXT
// ═══════════════════════════════════════════════════════════════════════════════

public record UpdateTranslationCommand(
    string  Code,
    string  LanguageCode,
    string  NewText,
    string? NewContext = null)
    : IRequest<Result<TranslationDto>>;

public sealed class UpdateTranslationCommandValidator
    : AbstractValidator<UpdateTranslationCommand>
{
    public UpdateTranslationCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");
        RuleFor(x => x.NewText).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateTranslationCommandHandler
    : IRequestHandler<UpdateTranslationCommand, Result<TranslationDto>>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public UpdateTranslationCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork            unitOfWork,
        ICacheService          cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Result<TranslationDto>> Handle(
        UpdateTranslationCommand request,
        CancellationToken        cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code        = codeResult.Value;
        var lang        = langResult.Value;
        var translation = await _repository.FindAsync(code, lang, cancellationToken);

        if (translation is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"Translation '{code.Value}' for language '{lang.Value}' was not found.");

        var updateResult = translation.UpdateText(request.NewText);
        if (updateResult.IsFailure) return updateResult.Error;

        translation.UpdateContext(request.NewContext);

        _repository.Update(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return CreateTranslationCommandHandler.MapToDto(translation);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DELETE TRANSLATION  (non-English only)
// ═══════════════════════════════════════════════════════════════════════════════

public record DeleteTranslationCommand(string Code, string LanguageCode) : IRequest<Result>;

public sealed class DeleteTranslationCommandHandler
    : IRequestHandler<DeleteTranslationCommand, Result>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public DeleteTranslationCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork            unitOfWork,
        ICacheService          cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Result> Handle(
        DeleteTranslationCommand request,
        CancellationToken        cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;

        // Protect English (default) entries — they are the fallback for all other languages
        if (lang.Language == "en")
            return Error.Conflict(
                "Translation.English.ProtectedDelete",
                $"Cannot delete the English translation for '{code.Value}'. " +
                "Remove all other language variants first, then delete the English entry via the admin bulk-delete tool.");

        var translation = await _repository.FindAsync(code, lang, cancellationToken);
        if (translation is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"Translation '{code.Value}' for language '{lang.Value}' was not found.");

        _repository.Remove(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return Result.Ok();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// MARK AS REVIEWED / UNREVIEWED
// ═══════════════════════════════════════════════════════════════════════════════

public record SetTranslationReviewedCommand(string Code, string LanguageCode, bool IsReviewed)
    : IRequest<Result>;

public sealed class SetTranslationReviewedCommandHandler
    : IRequestHandler<SetTranslationReviewedCommand, Result>
{
    private readonly ITranslationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public SetTranslationReviewedCommandHandler(
        ITranslationRepository repository,
        IUnitOfWork            unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        SetTranslationReviewedCommand request,
        CancellationToken             cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var translation = await _repository.FindAsync(
            codeResult.Value, langResult.Value, cancellationToken);

        if (translation is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"Translation '{codeResult.Value.Value}' for language '{langResult.Value.Value}' was not found.");

        if (request.IsReviewed) translation.MarkAsReviewed();
        else                    translation.MarkAsUnreviewed();

        _repository.Update(translation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// BULK UPSERT  (import / CI-CD pipeline)
// ═══════════════════════════════════════════════════════════════════════════════

public record BulkUpsertTranslationsCommand(
    IReadOnlyList<BulkTranslationItem> Items)
    : IRequest<Result<BulkUpsertResultDto>>;

public record BulkTranslationItem(
    string  Code,
    string  LanguageCode,
    string  Text,
    string? Context   = null,
    int?    MaxLength = null);

public sealed class BulkUpsertTranslationsCommandValidator
    : AbstractValidator<BulkUpsertTranslationsCommand>
{
    public BulkUpsertTranslationsCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.")
            .Must(i => i.Count <= 5000).WithMessage("Maximum 5,000 items per bulk operation.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Code).NotEmpty();
            item.RuleFor(i => i.LanguageCode).NotEmpty();
            item.RuleFor(i => i.Text).NotEmpty().MaximumLength(4000);
        });
    }
}

public sealed class BulkUpsertTranslationsCommandHandler
    : IRequestHandler<BulkUpsertTranslationsCommand, Result<BulkUpsertResultDto>>
{
    private readonly ITranslationRepository _translationRepo;
    private readonly ILanguageRepository    _languageRepo;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public BulkUpsertTranslationsCommandHandler(
        ITranslationRepository translationRepo,
        ILanguageRepository    languageRepo,
        IUnitOfWork            unitOfWork,
        ICacheService          cache)
    {
        _translationRepo = translationRepo;
        _languageRepo    = languageRepo;
        _unitOfWork      = unitOfWork;
        _cache           = cache;
    }

    public async Task<Result<BulkUpsertResultDto>> Handle(
        BulkUpsertTranslationsCommand request,
        CancellationToken             cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var errors  = new List<string>();
        var moduleLangPairs = new HashSet<(string, string)>();

        // Pre-load active language codes
        var activeLangs = (await _languageRepo.GetAllActiveAsync(cancellationToken))
            .Select(l => l.Code.ToLowerInvariant())
            .ToHashSet();

        foreach (var item in request.Items)
        {
            var codeResult = TranslationCode.From(item.Code);
            if (codeResult.IsFailure) { errors.Add($"{item.Code}: {codeResult.Error.Message}"); continue; }

            var langResult = LanguageCode.Create(item.LanguageCode);
            if (langResult.IsFailure) { errors.Add($"{item.Code}/{item.LanguageCode}: {langResult.Error.Message}"); continue; }

            var code = codeResult.Value;
            var lang = langResult.Value;

            if (!activeLangs.Contains(lang.Value.ToLowerInvariant()))
            {
                errors.Add($"{item.Code}/{lang.Value}: Language not found or inactive."); continue;
            }

            var existing = await _translationRepo.FindAsync(code, lang, cancellationToken);
            if (existing is null)
            {
                var createResult = Translation.Create(
                    SequentialGuid.NewSequentialGuidAtEnd(),
                    item.Code, item.LanguageCode, item.Text, item.Context, item.MaxLength);

                if (createResult.IsFailure) { errors.Add($"{item.Code}: {createResult.Error.Message}"); continue; }

                await _translationRepo.AddAsync(createResult.Value, cancellationToken);
                created++;
            }
            else
            {
                var updateResult = existing.UpdateText(item.Text);
                if (updateResult.IsFailure) { errors.Add($"{item.Code}: {updateResult.Error.Message}"); continue; }

                existing.UpdateContext(item.Context);
                _translationRepo.Update(existing);
                updated++;
            }

            moduleLangPairs.Add((code.Module, lang.Value));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate module caches
        foreach (var (module, lang) in moduleLangPairs)
            await _cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(module, lang), cancellationToken);

        return new BulkUpsertResultDto(created, updated, errors);
    }
}
