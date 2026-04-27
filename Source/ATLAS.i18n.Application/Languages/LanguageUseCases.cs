using Atlas.SharedKernel.Infrastructure.Primitives;
using FluentValidation;

namespace ATLAS.i18n.Application.Languages;

// ═══════════════════════════════════════════════════════════════════════════════
// QUERIES
// ═══════════════════════════════════════════════════════════════════════════════

public record GetAllLanguagesQuery(bool ActiveOnly = true)
    : IRequest<Result<IReadOnlyList<LanguageDto>>>,
      ICacheableRequest
{
    public string    CacheKey      => ActiveOnly ? I18nCacheKeys.LanguagesActive : I18nCacheKeys.LanguagesAll;
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public sealed class GetAllLanguagesQueryHandler
    : IRequestHandler<GetAllLanguagesQuery, Result<IReadOnlyList<LanguageDto>>>
{
    private readonly ILanguageRepository _repository;

    public GetAllLanguagesQueryHandler(ILanguageRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(
        GetAllLanguagesQuery request,
        CancellationToken    cancellationToken)
    {
        var languages = request.ActiveOnly
            ? await _repository.GetAllActiveAsync(cancellationToken)
            : await _repository.ListAllAsync(cancellationToken);

        return Result<IReadOnlyList<LanguageDto>>.Ok(
            languages.Select(MapToDto).ToList());
    }

    internal static LanguageDto MapToDto(Language l) =>
        new(l.Id, l.Code, l.Name, l.NativeName, l.IsDefault, l.IsActive,
            l.CreatedAt, l.CreatedBy, l.UpdatedAt, l.UpdatedBy);
}

// ─── Get single language ──────────────────────────────────────────────────────

public record GetLanguageQuery(string Code)
    : IRequest<Result<LanguageDto>>,
      ICacheableRequest
{
    public string    CacheKey      => I18nCacheKeys.Language(Code);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public sealed class GetLanguageQueryHandler
    : IRequestHandler<GetLanguageQuery, Result<LanguageDto>>
{
    private readonly ILanguageRepository _repository;

    public GetLanguageQueryHandler(ILanguageRepository repository)
        => _repository = repository;

    public async Task<Result<LanguageDto>> Handle(
        GetLanguageQuery  request,
        CancellationToken cancellationToken)
    {
        var language = await _repository.GetByCodeAsync(request.Code, cancellationToken);

        return language is null
            ? Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.")
            : GetAllLanguagesQueryHandler.MapToDto(language);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    bool   IsDefault = false)
    : IRequest<Result<LanguageDto>>;

public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(c => LanguageCode.Create(c).IsSuccess)
            .WithMessage("Code must be a valid BCP-47 tag (e.g. es-ES, en-US, ca-ES).");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateLanguageCommandHandler
    : IRequestHandler<CreateLanguageCommand, Result<LanguageDto>>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public CreateLanguageCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork         unitOfWork,
        ICacheService       cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Result<LanguageDto>> Handle(
        CreateLanguageCommand request,
        CancellationToken     cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code, cancellationToken))
            return Error.Conflict(
                "Language.Duplicate",
                $"Language '{request.Code}' already exists.");

        var createResult = Language.Create(
            SequentialGuid.NewSequentialGuidAtEnd(),
            request.Code,
            request.Name,
            request.NativeName,
            request.IsDefault);

        if (createResult.IsFailure) return createResult.Error;

        await _repository.AddAsync(createResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateLanguageCachesAsync(cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(createResult.Value);
    }

    private async Task InvalidateLanguageCachesAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesAll, ct);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesActive, ct);
    }
}

// ─── Update Language ──────────────────────────────────────────────────────────

public record UpdateLanguageCommand(string Code, string Name, string NativeName)
    : IRequest<Result<LanguageDto>>;

public sealed class UpdateLanguageCommandHandler
    : IRequestHandler<UpdateLanguageCommand, Result<LanguageDto>>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public UpdateLanguageCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork         unitOfWork,
        ICacheService       cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Result<LanguageDto>> Handle(
        UpdateLanguageCommand request,
        CancellationToken     cancellationToken)
    {
        var language = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.");

        var updateResult = language.Update(request.Name, request.NativeName);
        if (updateResult.IsFailure) return updateResult.Error;

        _repository.Update(language);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.Language(request.Code), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesActive, cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(language);
    }
}

// ─── Activate / Deactivate Language ──────────────────────────────────────────

public record SetLanguageActiveCommand(string Code, bool IsActive) : IRequest<r>;

public sealed class SetLanguageActiveCommandHandler
    : IRequestHandler<SetLanguageActiveCommand, Result>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public SetLanguageActiveCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork         unitOfWork,
        ICacheService       cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<r> Handle(
        SetLanguageActiveCommand request,
        CancellationToken        cancellationToken)
    {
        var language = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.");

        Result opResult;
        if (request.IsActive)
        {
            language.Activate();
            opResult = Result.Ok();
        }
        else
        {
            opResult = language.Deactivate(); // returns Conflict if IsDefault
        }

        if (opResult.IsFailure) return opResult.Error;

        _repository.Update(language);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.Language(request.Code), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}

// ─── Set Default Language ────────────────────────────────────────────────────

public record SetDefaultLanguageCommand(string Code) : IRequest<r>;

public sealed class SetDefaultLanguageCommandHandler
    : IRequestHandler<SetDefaultLanguageCommand, Result>
{
    private readonly ILanguageDomainService _domainService;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public SetDefaultLanguageCommandHandler(
        ILanguageDomainService domainService,
        IUnitOfWork            unitOfWork,
        ICacheService          cache)
    {
        _domainService = domainService;
        _unitOfWork    = unitOfWork;
        _cache         = cache;
    }

    public async Task<r> Handle(
        SetDefaultLanguageCommand request,
        CancellationToken         cancellationToken)
    {
        var result = await _domainService.SetDefaultLanguageAsync(request.Code, cancellationToken);
        if (result.IsFailure) return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.LanguageDefault, cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}
