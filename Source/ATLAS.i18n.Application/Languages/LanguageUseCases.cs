using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.Services;
using ATLAS.i18n.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ATLAS.i18n.Application.Languages.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
// GET ALL LANGUAGES
// ═══════════════════════════════════════════════════════════════════════════════

public record GetAllLanguagesQuery(bool ActiveOnly = true)
    : IRequest<IReadOnlyList<LanguageDto>>;

public sealed class GetAllLanguagesQueryHandler
    : IRequestHandler<GetAllLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    private readonly ILanguageRepository _repository;
    private readonly ICacheService       _cache;

    public GetAllLanguagesQueryHandler(
        ILanguageRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<IReadOnlyList<LanguageDto>> Handle(
        GetAllLanguagesQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.ActiveOnly
            ? CacheKeys.LanguagesActive
            : CacheKeys.LanguagesAll;

        var cached = await _cache.GetAsync<List<LanguageDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var languages = request.ActiveOnly
            ? await _repository.GetAllActiveAsync(cancellationToken)
            : await _repository.GetAllAsync(cancellationToken);

        var dtos = languages.Select(MapToDto).ToList();

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(30), cancellationToken);

        return dtos;
    }

    internal static LanguageDto MapToDto(Language l) =>
        new(l.Id.Value, l.Name, l.NativeName, l.CultureTag,
            l.IsDefault, l.IsActive, l.CreatedAt, l.UpdatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET SINGLE LANGUAGE
// ═══════════════════════════════════════════════════════════════════════════════

public record GetLanguageQuery(string Code) : IRequest<LanguageDto>;

public sealed class GetLanguageQueryHandler
    : IRequestHandler<GetLanguageQuery, LanguageDto>
{
    private readonly ILanguageRepository _repository;
    private readonly ICacheService       _cache;

    public GetLanguageQueryHandler(ILanguageRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<LanguageDto> Handle(
        GetLanguageQuery request,
        CancellationToken cancellationToken)
    {
        var code     = LanguageCode.From(request.Code);
        var cacheKey = CacheKeys.Language(code.Value);

        var cached = await _cache.GetAsync<LanguageDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var language = await _repository.GetByCodeAsync(code, cancellationToken)
            ?? throw new LanguageNotFoundException(code.Value);

        var dto = GetAllLanguagesQueryHandler.MapToDto(language);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30), cancellationToken);

        return dto;
    }
}

namespace ATLAS.i18n.Application.Languages.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// CREATE LANGUAGE
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    string CultureTag,
    bool IsDefault = false)
    : IRequest<LanguageDto>;

public sealed class CreateLanguageCommandValidator
    : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().Length(2).Matches("^[a-zA-Z]{2}$")
            .WithMessage("Language code must be a 2-letter ISO 639-1 code.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CultureTag)
            .NotEmpty().MaximumLength(10)
            .Matches(@"^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("CultureTag must follow BCP-47 format, e.g. 'en-US', 'es-ES'.");
    }
}

public sealed class CreateLanguageCommandHandler
    : IRequestHandler<CreateLanguageCommand, LanguageDto>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public CreateLanguageCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<LanguageDto> Handle(
        CreateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var code = LanguageCode.From(request.Code);

        if (await _repository.ExistsAsync(code, cancellationToken))
            throw new I18nDomainException(
                $"Language '{code.Value}' already exists.");

        var language = Language.Create(
            request.Code, request.Name, request.NativeName,
            request.CultureTag, request.IsDefault);

        _repository.Add(language);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(language);
    }

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(CacheKeys.LanguagesAll, ct);
        await _cache.RemoveAsync(CacheKeys.LanguagesActive, ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// UPDATE LANGUAGE
// ═══════════════════════════════════════════════════════════════════════════════

public record UpdateLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    string CultureTag)
    : IRequest<LanguageDto>;

public sealed class UpdateLanguageCommandHandler
    : IRequestHandler<UpdateLanguageCommand, LanguageDto>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public UpdateLanguageCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<LanguageDto> Handle(
        UpdateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var code     = LanguageCode.From(request.Code);
        var language = await _repository.GetByCodeAsync(code, cancellationToken)
            ?? throw new LanguageNotFoundException(code.Value);

        language.Update(request.Name, request.NativeName, request.CultureTag);
        _repository.Update(language);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Language(code.Value), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesActive, cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(language);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ACTIVATE / DEACTIVATE LANGUAGE
// ═══════════════════════════════════════════════════════════════════════════════

public record SetLanguageActiveCommand(string Code, bool IsActive) : IRequest<Unit>;

public sealed class SetLanguageActiveCommandHandler
    : IRequestHandler<SetLanguageActiveCommand, Unit>
{
    private readonly ILanguageRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ICacheService       _cache;

    public SetLanguageActiveCommandHandler(
        ILanguageRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Unit> Handle(
        SetLanguageActiveCommand request,
        CancellationToken cancellationToken)
    {
        var code     = LanguageCode.From(request.Code);
        var language = await _repository.GetByCodeAsync(code, cancellationToken)
            ?? throw new LanguageNotFoundException(code.Value);

        if (request.IsActive) language.Activate();
        else                  language.Deactivate();  // throws if IsDefault

        _repository.Update(language);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.Language(code.Value), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesActive, cancellationToken);

        return Unit.Value;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SET DEFAULT LANGUAGE
// ═══════════════════════════════════════════════════════════════════════════════

public record SetDefaultLanguageCommand(string Code) : IRequest<Unit>;

public sealed class SetDefaultLanguageCommandHandler
    : IRequestHandler<SetDefaultLanguageCommand, Unit>
{
    private readonly ILanguageDomainService _domainService;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICacheService          _cache;

    public SetDefaultLanguageCommandHandler(
        ILanguageDomainService domainService,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _domainService = domainService;
        _unitOfWork    = unitOfWork;
        _cache         = cache;
    }

    public async Task<Unit> Handle(
        SetDefaultLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var code = LanguageCode.From(request.Code);
        await _domainService.SetDefaultLanguageAsync(code, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.LanguageDefault, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesAll, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LanguagesActive, cancellationToken);

        return Unit.Value;
    }
}
