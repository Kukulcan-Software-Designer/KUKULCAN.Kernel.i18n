using FluentValidation;
using ATLAS.i18n.Application.Translations.Queries;

namespace ATLAS.i18n.Application.Translations.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
// GET SINGLE TRANSLATION  (hot path)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns the translated text for a code + language combination, applying the
/// BCP-47 fallback chain automatically (<c>es-ES → es → en</c>).
/// </summary>
/// <param name="Code">Translation code, e.g. <c>"CRM0001"</c>.</param>
/// <param name="LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
public record GetTranslationQuery(string Code, string LanguageCode)
    : IRequest<Result<TranslationLookupDto>>;

public sealed class GetTranslationQueryValidator : AbstractValidator<GetTranslationQuery>
{
    public GetTranslationQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Translation code is required.")
            .Must(c => TranslationCode.From(c).IsSuccess)
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag (e.g. es-ES, en-US).");
    }
}

public sealed class GetTranslationQueryHandler
    : IRequestHandler<GetTranslationQuery, Result<TranslationLookupDto>>
{
    private readonly ITranslationLookupService _lookupService;
    private readonly ICacheService             _cache;

    public GetTranslationQueryHandler(
        ITranslationLookupService lookupService,
        ICacheService             cache)
    {
        _lookupService = lookupService;
        _cache         = cache;
    }

    public async Task<Result<TranslationLookupDto>> Handle(
        GetTranslationQuery request,
        CancellationToken   cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;
        var key  = I18nCacheKeys.Translation(code.Value, lang.Value);

        // Use SharedKernel's GetOrCreate — handles cache-aside in one call
        var dto = await _cache.GetOrCreateAsync<TranslationLookupDto?>(
            key,
            async ct =>
            {
                var resolved = await _lookupService.ResolveAsync(code, lang, ct);
                if (resolved.IsFailure) return null;

                var (text, actualLang, isFallback) = resolved.Value;
                return new TranslationLookupDto(
                    code.Value, lang.Value, text, isFallback, actualLang);
            },
            expiry: TimeSpan.FromHours(1),
            cancellationToken: cancellationToken);

        if (dto is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"No translation found for '{code.Value}' in language '{lang.Value}'.");

        return dto;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET MODULE STRING TABLE  (bulk / client-side caching)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns all translations for a module and language as a flat dictionary.
/// Missing individual translations are filled by the BCP-47 fallback chain.
/// </summary>
/// <param name="Module">Module prefix, e.g. <c>"CRM"</c>, <c>"PIM"</c>.</param>
/// <param name="LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
public record GetModuleTranslationsQuery(string Module, string LanguageCode)
    : IRequest<Result<TranslationMapDto>>,
      ICacheableRequest
{
    public string    CacheKey      => I18nCacheKeys.ModuleTranslations(Module, LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}

public sealed class GetModuleTranslationsQueryValidator
    : AbstractValidator<GetModuleTranslationsQuery>
{
    public GetModuleTranslationsQueryValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty()
            .MinimumLength(TranslationCode.MinModuleLength)
            .MaximumLength(TranslationCode.MaxModuleLength)
            .Matches("^[a-zA-Z]+$").WithMessage("Module must contain only letters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");
    }
}

public sealed class GetModuleTranslationsQueryHandler
    : IRequestHandler<GetModuleTranslationsQuery, Result<TranslationMapDto>>
{
    private readonly ITranslationRepository _repository;

    public GetModuleTranslationsQueryHandler(ITranslationRepository repository)
        => _repository = repository;

    public async Task<Result<TranslationMapDto>> Handle(
        GetModuleTranslationsQuery request,
        CancellationToken          cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang   = langResult.Value;
        var module = request.Module.ToUpperInvariant();

        // Load requested language
        var requested = await _repository.GetByModuleAndLanguageAsync(module, lang, cancellationToken);
        var map       = requested.ToDictionary(t => t.Code.Value, t => t.Text);

        // Walk the fallback chain and fill gaps for any missing codes
        foreach (var fallbackTag in lang.FallbackChain.Skip(1)) // skip the first (already loaded)
        {
            var fbLangResult = LanguageCode.Create(fallbackTag);
            if (fbLangResult.IsFailure) continue;

            var fallback = await _repository.GetByModuleAndLanguageAsync(
                module, fbLangResult.Value, cancellationToken);

            foreach (var t in fallback)
            {
                if (!map.ContainsKey(t.Code.Value))
                    map[t.Code.Value] = t.Text;
            }
        }

        return new TranslationMapDto(lang.Value, module, map);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET TRANSLATIONS PAGED  (admin / backoffice)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns a paged list of translations for admin tooling.
/// Uses <see cref="PaginationRequest"/> from <c>Atlas.SharedKernel.Infrastructure</c>.
/// </summary>
public record GetTranslationsPagedQuery(
    PaginationRequest Pagination,
    string?           ModuleFilter   = null,
    string?           LanguageFilter = null)
    : IRequest<Result<PagedResult<TranslationDto>>>;

public sealed class GetTranslationsPagedQueryValidator
    : AbstractValidator<GetTranslationsPagedQuery>
{
    public GetTranslationsPagedQueryValidator()
    {
        When(x => x.ModuleFilter is not null, () =>
            RuleFor(x => x.ModuleFilter!)
                .MinimumLength(TranslationCode.MinModuleLength)
                .MaximumLength(TranslationCode.MaxModuleLength)
                .Matches("^[a-zA-Z]+$"));

        When(x => x.LanguageFilter is not null, () =>
            RuleFor(x => x.LanguageFilter!)
                .Must(lc => LanguageCode.Create(lc).IsSuccess)
                .WithMessage("LanguageFilter must be a valid BCP-47 tag."));
    }
}

public sealed class GetTranslationsPagedQueryHandler
    : IRequestHandler<GetTranslationsPagedQuery, Result<PagedResult<TranslationDto>>>
{
    private readonly ITranslationRepository _repository;

    public GetTranslationsPagedQueryHandler(ITranslationRepository repository)
        => _repository = repository;

    public async Task<Result<PagedResult<TranslationDto>>> Handle(
        GetTranslationsPagedQuery request,
        CancellationToken         cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.Pagination.Page,
            request.Pagination.PageSize,
            request.ModuleFilter?.ToUpperInvariant(),
            request.LanguageFilter?.ToLowerInvariant(),
            cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        // Use SharedKernel's PagedResult.Create
        return PagedResult<TranslationDto>.Create(dtos, total, request.Pagination);
    }

    // ── GET VARIANTS ──────────────────────────────────────────────────────────

    internal static TranslationDto MapToDto(Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed,
            t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy);
}

// ─── Get all variants for a code ─────────────────────────────────────────────

public record GetTranslationVariantsQuery(string Code)
    : IRequest<Result<IReadOnlyList<TranslationDto>>>;

public sealed class GetTranslationVariantsQueryHandler
    : IRequestHandler<GetTranslationVariantsQuery, Result<IReadOnlyList<TranslationDto>>>
{
    private readonly ITranslationRepository _repository;

    public GetTranslationVariantsQueryHandler(ITranslationRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<TranslationDto>>> Handle(
        GetTranslationVariantsQuery request,
        CancellationToken           cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var items = await _repository.GetVariantsAsync(codeResult.Value, cancellationToken);
        return items.Select(GetTranslationsPagedQueryHandler.MapToDto)
                    .ToList()
                    .AsReadOnly()
                    .ToResult();
    }
}

// ── Helper ────────────────────────────────────────────────────────────────────

file static class EnumerableResultExtensions
{
    internal static Result<IReadOnlyList<T>> ToResult<T>(this IReadOnlyList<T> list)
        => Result<IReadOnlyList<T>>.Ok(list);
}
