using Atlas.SharedKernel.Infrastructure.Primitives;
using FluentValidation;

namespace ATLAS.i18n.Application.Locales;

// ═══════════════════════════════════════════════════════════════════════════════
// LOCALE CONFIGURATION QUERIES
// ═══════════════════════════════════════════════════════════════════════════════

public record GetLocaleConfigurationQuery(string LanguageCode)
    : IRequest<Result<LocaleConfigurationDto>>,
      ICacheableRequest
{
    public string    CacheKey      => I18nCacheKeys.LocaleConfig(LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromHours(6);
}

public sealed class GetLocaleConfigurationQueryHandler
    : IRequestHandler<GetLocaleConfigurationQuery, Result<LocaleConfigurationDto>>
{
    private readonly ILocaleConfigurationRepository _repository;

    public GetLocaleConfigurationQueryHandler(ILocaleConfigurationRepository repository)
        => _repository = repository;

    public async Task<Result<LocaleConfigurationDto>> Handle(
        GetLocaleConfigurationQuery request,
        CancellationToken           cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var config = await _repository.GetByLanguageAsync(langResult.Value, cancellationToken);

        return config is null
            ? Error.NotFound(
                "LocaleConfig.NotFound",
                $"No locale configuration found for language '{request.LanguageCode}'.")
            : MapToDto(config);
    }

    internal static LocaleConfigurationDto MapToDto(LocaleConfiguration c) =>
        new(c.LanguageCode.Value, c.DateFormat, c.ShortDateFormat, c.TimeFormat,
            c.DateTimeFormat, c.FirstDayOfWeek.ToString(),
            c.DecimalSeparator.ToString(), c.ThousandsSeparator.ToString(),
            c.DecimalPlaces, c.CurrencyDecimalPlaces,
            c.CreatedAt, c.UpdatedAt);
}

// ─── Get all locale configurations ───────────────────────────────────────────

public record GetAllLocaleConfigurationsQuery : IRequest<Result<IReadOnlyList<LocaleConfigurationDto>>>;

public sealed class GetAllLocaleConfigurationsQueryHandler
    : IRequestHandler<GetAllLocaleConfigurationsQuery, Result<IReadOnlyList<LocaleConfigurationDto>>>
{
    private readonly ILocaleConfigurationRepository _repository;

    public GetAllLocaleConfigurationsQueryHandler(ILocaleConfigurationRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<LocaleConfigurationDto>>> Handle(
        GetAllLocaleConfigurationsQuery request,
        CancellationToken               cancellationToken)
    {
        var configs = await _repository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<LocaleConfigurationDto>>.Ok(
            configs.Select(GetLocaleConfigurationQueryHandler.MapToDto).ToList());
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// LOCALE CONFIGURATION COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

public record UpsertLocaleConfigurationCommand(
    string         LanguageCode,
    string         DateFormat,
    string         ShortDateFormat,
    string         TimeFormat,
    string         DateTimeFormat,
    string         FirstDayOfWeek,
    string         DecimalSeparator,
    string         ThousandsSeparator,
    int            DecimalPlaces         = 2,
    int            CurrencyDecimalPlaces = 2)
    : IRequest<Result<LocaleConfigurationDto>>;

public sealed class UpsertLocaleConfigurationCommandValidator
    : AbstractValidator<UpsertLocaleConfigurationCommand>
{
    public UpsertLocaleConfigurationCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");

        RuleFor(x => x.DateFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShortDateFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TimeFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateTimeFormat).NotEmpty().MaximumLength(100);

        RuleFor(x => x.FirstDayOfWeek)
            .NotEmpty()
            .Must(v => Enum.TryParse<FirstDayOfWeek>(v, true, out _))
            .WithMessage("FirstDayOfWeek must be 'Sunday', 'Monday', or 'Saturday'.");

        RuleFor(x => x.DecimalSeparator).NotEmpty().Length(1);
        RuleFor(x => x.ThousandsSeparator).NotEmpty().Length(1);

        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different characters.");

        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 10);
        RuleFor(x => x.CurrencyDecimalPlaces).InclusiveBetween(0, 10);
    }
}

public sealed class UpsertLocaleConfigurationCommandHandler
    : IRequestHandler<UpsertLocaleConfigurationCommand, Result<LocaleConfigurationDto>>
{
    private readonly ILocaleConfigurationRepository _repository;
    private readonly ILanguageRepository            _languageRepo;
    private readonly IUnitOfWork                    _unitOfWork;
    private readonly ICacheService                  _cache;

    public UpsertLocaleConfigurationCommandHandler(
        ILocaleConfigurationRepository repository,
        ILanguageRepository            languageRepo,
        IUnitOfWork                    unitOfWork,
        ICacheService                  cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<Result<LocaleConfigurationDto>> Handle(
        UpsertLocaleConfigurationCommand request,
        CancellationToken                cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang = langResult.Value;

        // Language must exist
        if (!await _languageRepo.ExistsByCodeAsync(lang.Value, cancellationToken))
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        var firstDay   = Enum.Parse<FirstDayOfWeek>(request.FirstDayOfWeek, true);
        var decSep     = request.DecimalSeparator[0];
        var thousSep   = request.ThousandsSeparator[0];
        var existing   = await _repository.GetByLanguageAsync(lang, cancellationToken);

        LocaleConfiguration config;

        if (existing is null)
        {
            var createResult = LocaleConfiguration.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                request.LanguageCode, request.DateFormat, request.ShortDateFormat,
                request.TimeFormat, request.DateTimeFormat, firstDay,
                decSep, thousSep, request.DecimalPlaces, request.CurrencyDecimalPlaces);

            if (createResult.IsFailure) return createResult.Error;

            await _repository.AddAsync(createResult.Value, cancellationToken);
            config = createResult.Value;
        }
        else
        {
            var updateResult = existing.Update(
                request.DateFormat, request.ShortDateFormat,
                request.TimeFormat, request.DateTimeFormat, firstDay,
                decSep, thousSep, request.DecimalPlaces, request.CurrencyDecimalPlaces);

            if (updateResult.IsFailure) return updateResult.Error;

            _repository.Update(existing);
            config = existing;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.LocaleConfig(lang.Value), cancellationToken);

        return GetLocaleConfigurationQueryHandler.MapToDto(config);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CURRENCY FORMAT QUERIES
// ═══════════════════════════════════════════════════════════════════════════════

public record GetCurrencyFormatsQuery(string LanguageCode)
    : IRequest<Result<IReadOnlyList<CurrencyFormatDto>>>,
      ICacheableRequest
{
    public string    CacheKey      => I18nCacheKeys.CurrencyFormats(LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromHours(6);
}

public sealed class GetCurrencyFormatsQueryHandler
    : IRequestHandler<GetCurrencyFormatsQuery, Result<IReadOnlyList<CurrencyFormatDto>>>
{
    private readonly ICurrencyFormatRepository _repository;

    public GetCurrencyFormatsQueryHandler(ICurrencyFormatRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<CurrencyFormatDto>>> Handle(
        GetCurrencyFormatsQuery request,
        CancellationToken       cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var formats = await _repository.GetByLanguageAsync(langResult.Value, cancellationToken);

        return Result<IReadOnlyList<CurrencyFormatDto>>.Ok(
            formats.Select(MapToDto).ToList());
    }

    internal static CurrencyFormatDto MapToDto(CurrencyFormat f) =>
        new(f.Id, f.LanguageCode.Value, f.CurrencyCode, f.CurrencyName,
            f.Symbol, f.SymbolPosition.ToString(), f.SpaceBetweenSymbolAndAmount,
            f.DecimalSeparator.ToString(), f.ThousandsSeparator.ToString(),
            f.DecimalPlaces, f.NegativePattern,
            f.Format(1_234.56m),   // pre-formatted example
            f.CreatedAt, f.UpdatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════════
// CURRENCY FORMAT COMMANDS
// ═══════════════════════════════════════════════════════════════════════════════

public record UpsertCurrencyFormatCommand(
    string                 LanguageCode,
    string                 CurrencyCode,
    string                 CurrencyName,
    string                 Symbol,
    string                 SymbolPosition,
    bool                   SpaceBetweenSymbolAndAmount,
    string                 DecimalSeparator,
    string                 ThousandsSeparator,
    int                    DecimalPlaces,
    string                 NegativePattern = "-{symbol}{amount}")
    : IRequest<Result<CurrencyFormatDto>>;

public sealed class UpsertCurrencyFormatCommandValidator
    : AbstractValidator<UpsertCurrencyFormatCommand>
{
    public UpsertCurrencyFormatCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3)
            .Matches("^[a-zA-Z]{3}$")
            .WithMessage("CurrencyCode must be a 3-letter ISO 4217 code (e.g. USD, EUR).");

        RuleFor(x => x.CurrencyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(5);

        RuleFor(x => x.SymbolPosition)
            .NotEmpty()
            .Must(v => Enum.TryParse<CurrencySymbolPosition>(v, true, out _))
            .WithMessage("SymbolPosition must be 'Before' or 'After'.");

        RuleFor(x => x.DecimalSeparator).NotEmpty().Length(1);
        RuleFor(x => x.ThousandsSeparator).NotEmpty().Length(1);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 10);

        RuleFor(x => x.NegativePattern)
            .NotEmpty()
            .Must(p => p.Contains("{amount}"))
            .WithMessage("NegativePattern must contain the {amount} placeholder.");

        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different characters.");
    }
}

public sealed class UpsertCurrencyFormatCommandHandler
    : IRequestHandler<UpsertCurrencyFormatCommand, Result<CurrencyFormatDto>>
{
    private readonly ICurrencyFormatRepository _repository;
    private readonly ILanguageRepository       _languageRepo;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ICacheService             _cache;

    public UpsertCurrencyFormatCommandHandler(
        ICurrencyFormatRepository repository,
        ILanguageRepository       languageRepo,
        IUnitOfWork               unitOfWork,
        ICacheService             cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<Result<CurrencyFormatDto>> Handle(
        UpsertCurrencyFormatCommand request,
        CancellationToken           cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang     = langResult.Value;
        var currency = request.CurrencyCode.ToUpperInvariant();

        if (!await _languageRepo.ExistsByCodeAsync(lang.Value, cancellationToken))
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        var symPos   = Enum.Parse<CurrencySymbolPosition>(request.SymbolPosition, true);
        var existing = await _repository.FindAsync(lang, currency, cancellationToken);

        CurrencyFormat format;

        if (existing is null)
        {
            var createResult = CurrencyFormat.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                request.LanguageCode, currency, request.CurrencyName,
                request.Symbol, symPos, request.SpaceBetweenSymbolAndAmount,
                request.DecimalSeparator[0], request.ThousandsSeparator[0],
                request.DecimalPlaces, request.NegativePattern);

            if (createResult.IsFailure) return createResult.Error;

            await _repository.AddAsync(createResult.Value, cancellationToken);
            format = createResult.Value;
        }
        else
        {
            var updateResult = existing.Update(
                request.CurrencyName, request.Symbol, symPos,
                request.SpaceBetweenSymbolAndAmount,
                request.DecimalSeparator[0], request.ThousandsSeparator[0],
                request.DecimalPlaces, request.NegativePattern);

            if (updateResult.IsFailure) return updateResult.Error;

            _repository.Update(existing);
            format = existing;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.CurrencyFormats(lang.Value), cancellationToken);

        return GetCurrencyFormatsQueryHandler.MapToDto(format);
    }
}

// ─── Delete Currency Format ───────────────────────────────────────────────────

public record DeleteCurrencyFormatCommand(string LanguageCode, string CurrencyCode) : IRequest<r>;

public sealed class DeleteCurrencyFormatCommandHandler
    : IRequestHandler<DeleteCurrencyFormatCommand, Result>
{
    private readonly ICurrencyFormatRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ICacheService             _cache;

    public DeleteCurrencyFormatCommandHandler(
        ICurrencyFormatRepository repository,
        IUnitOfWork               unitOfWork,
        ICacheService             cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<r> Handle(
        DeleteCurrencyFormatCommand request,
        CancellationToken           cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang     = langResult.Value;
        var currency = request.CurrencyCode.ToUpperInvariant();

        var format = await _repository.FindAsync(lang, currency, cancellationToken);
        if (format is null)
            return Error.NotFound(
                "CurrencyFormat.NotFound",
                $"No currency format for '{currency}' in language '{lang.Value}'.");

        _repository.Remove(format);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(I18nCacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await _cache.RemoveAsync(I18nCacheKeys.CurrencyFormats(lang.Value), cancellationToken);

        return Result.Ok();
    }
}
