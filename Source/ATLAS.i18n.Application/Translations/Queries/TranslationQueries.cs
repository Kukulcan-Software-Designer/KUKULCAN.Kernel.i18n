using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.Services;
using ATLAS.i18n.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ATLAS.i18n.Application.Translations.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
// GET SINGLE TRANSLATION
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Retrieves the text for a specific translation code and language.
/// Falls back to English automatically if the requested language is unavailable.
/// </summary>
/// <param name="Code">Translation code, e.g. "CRM0001".</param>
/// <param name="LanguageCode">ISO 639-1 language code, e.g. "ES".</param>
public record GetTranslationQuery(string Code, string LanguageCode)
    : IRequest<TranslationLookupDto>;

public sealed class GetTranslationQueryValidator : AbstractValidator<GetTranslationQuery>
{
    public GetTranslationQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Translation code is required.")
            .Must(c =>
            {
                try { TranslationCode.From(c); return true; }
                catch { return false; }
            })
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Length(2).WithMessage("Language code must be exactly 2 characters.")
            .Matches("^[a-zA-Z]{2}$").WithMessage("Language code must contain only letters.");
    }
}

public sealed class GetTranslationQueryHandler
    : IRequestHandler<GetTranslationQuery, TranslationLookupDto>
{
    private readonly ITranslationRepository _repository;
    private readonly ICacheService _cache;

    public GetTranslationQueryHandler(
        ITranslationRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<TranslationLookupDto> Handle(
        GetTranslationQuery request,
        CancellationToken cancellationToken)
    {
        var code = TranslationCode.From(request.Code);
        var lang = LanguageCode.From(request.LanguageCode);

        var cacheKey = CacheKeys.Translation(code.Value, lang.Value);
        var cached   = await _cache.GetAsync<TranslationLookupDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        // 1. Try exact language
        var translation = await _repository.FindAsync(code, lang, cancellationToken);
        var isFallback  = false;

        // 2. Fall back to English
        if (translation is null && lang != LanguageCode.English)
        {
            translation = await _repository.FindAsync(code, LanguageCode.English, cancellationToken);
            isFallback  = translation is not null;
        }

        if (translation is null)
            throw new TranslationNotFoundException(code.Value, lang.Value);

        var dto = new TranslationLookupDto(
            translation.Code.Value,
            translation.LanguageCode.Value,
            translation.Text,
            isFallback);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);

        return dto;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET TRANSLATIONS BY MODULE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns all translations for a given module and language as a flat dictionary.
/// Useful for client-side caching — the client can download an entire module's
/// strings in one call (e.g. all CRM strings for ES).
/// Missing translations fall back to English individually.
/// </summary>
/// <param name="Module">Module prefix, e.g. "CRM", "PIM".</param>
/// <param name="LanguageCode">ISO 639-1 language code, e.g. "ES".</param>
public record GetTranslationsByModuleQuery(string Module, string LanguageCode)
    : IRequest<TranslationMapDto>;

public sealed class GetTranslationsByModuleQueryValidator
    : AbstractValidator<GetTranslationsByModuleQuery>
{
    public GetTranslationsByModuleQueryValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty()
            .MinimumLength(TranslationCode.MinModuleLength)
            .MaximumLength(TranslationCode.MaxModuleLength)
            .Matches("^[a-zA-Z]+$").WithMessage("Module must contain only letters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[a-zA-Z]{2}$");
    }
}

public sealed class GetTranslationsByModuleQueryHandler
    : IRequestHandler<GetTranslationsByModuleQuery, TranslationMapDto>
{
    private readonly ITranslationRepository _repository;
    private readonly ICacheService _cache;

    public GetTranslationsByModuleQueryHandler(
        ITranslationRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<TranslationMapDto> Handle(
        GetTranslationsByModuleQuery request,
        CancellationToken cancellationToken)
    {
        var lang      = LanguageCode.From(request.LanguageCode);
        var module    = request.Module.ToUpperInvariant();
        var cacheKey  = CacheKeys.TranslationsModule(module, lang.Value);

        var cached = await _cache.GetAsync<TranslationMapDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        // Load requested language + English (for fallback)
        var requested = await _repository.GetByModuleAndLanguageAsync(module, lang, cancellationToken);
        var fallbackNeeded = lang != LanguageCode.English;
        var englishMap = fallbackNeeded
            ? (await _repository.GetByModuleAndLanguageAsync(module, LanguageCode.English, cancellationToken))
                .ToDictionary(t => t.Code.Value, t => t.Text)
            : new Dictionary<string, string>();

        // Merge: requested language wins; English fills gaps
        var merged = requested.ToDictionary(t => t.Code.Value, t => t.Text);
        foreach (var (key, value) in englishMap)
        {
            if (!merged.ContainsKey(key))
                merged[key] = value;
        }

        var dto = new TranslationMapDto(lang.Value, module, merged);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);

        return dto;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET TRANSLATIONS PAGED (admin / backoffice)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns a paged list of translations. Intended for admin tooling.
/// </summary>
public record GetTranslationsPagedQuery(
    int PageNumber   = 1,
    int PageSize     = 50,
    string? Module   = null,
    string? Language = null)
    : IRequest<PagedResult<TranslationDto>>;

public sealed class GetTranslationsPagedQueryValidator
    : AbstractValidator<GetTranslationsPagedQuery>
{
    public GetTranslationsPagedQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);

        When(x => x.Module is not null, () =>
            RuleFor(x => x.Module!)
                .MinimumLength(TranslationCode.MinModuleLength)
                .MaximumLength(TranslationCode.MaxModuleLength)
                .Matches("^[a-zA-Z]+$"));

        When(x => x.Language is not null, () =>
            RuleFor(x => x.Language!)
                .Length(2)
                .Matches("^[a-zA-Z]{2}$"));
    }
}

public sealed class GetTranslationsPagedQueryHandler
    : IRequestHandler<GetTranslationsPagedQuery, PagedResult<TranslationDto>>
{
    private readonly ITranslationRepository _repository;

    public GetTranslationsPagedQueryHandler(ITranslationRepository repository)
        => _repository = repository;

    public async Task<PagedResult<TranslationDto>> Handle(
        GetTranslationsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Module?.ToUpperInvariant(),
            request.Language?.ToUpperInvariant(),
            cancellationToken);

        var dtos = items.Select(t => new TranslationDto(
            t.Id,
            t.Code.Value,
            t.Code.Module,
            t.LanguageCode.Value,
            t.Text,
            t.Context,
            t.MaxLength,
            t.IsReviewed,
            t.CreatedAt,
            t.UpdatedAt)).ToList();

        return new PagedResult<TranslationDto>(dtos, total, request.PageNumber, request.PageSize);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET ALL VARIANTS FOR A CODE
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns all language variants for a single translation code.
/// Useful in admin to see which languages are missing a translation.
/// </summary>
public record GetTranslationVariantsQuery(string Code)
    : IRequest<IReadOnlyList<TranslationDto>>;

public sealed class GetTranslationVariantsQueryHandler
    : IRequestHandler<GetTranslationVariantsQuery, IReadOnlyList<TranslationDto>>
{
    private readonly ITranslationRepository _repository;

    public GetTranslationVariantsQueryHandler(ITranslationRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<TranslationDto>> Handle(
        GetTranslationVariantsQuery request,
        CancellationToken cancellationToken)
    {
        var code  = TranslationCode.From(request.Code);
        var items = await _repository.GetAllVariantsAsync(code, cancellationToken);

        return items.Select(t => new TranslationDto(
            t.Id,
            t.Code.Value,
            t.Code.Module,
            t.LanguageCode.Value,
            t.Text,
            t.Context,
            t.MaxLength,
            t.IsReviewed,
            t.CreatedAt,
            t.UpdatedAt)).ToList();
    }
}
