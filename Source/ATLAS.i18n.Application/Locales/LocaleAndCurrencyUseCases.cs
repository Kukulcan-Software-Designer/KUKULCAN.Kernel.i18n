using ATLAS.i18n.Application.Common.DTOs;
using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ATLAS.i18n.Application.Locales.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
// GET LOCALE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════

public record GetLocaleConfigurationQuery(string LanguageCode)
    : IRequest<LocaleConfigurationDto>;

public sealed class GetLocaleConfigurationQueryHandler
    : IRequestHandler<GetLocaleConfigurationQuery, LocaleConfigurationDto>
{
    private readonly ILocaleConfigurationRepository _repository;
    private readonly ICacheService                  _cache;

    public GetLocaleConfigurationQueryHandler(
        ILocaleConfigurationRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<LocaleConfigurationDto> Handle(
        GetLocaleConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var lang     = LanguageCode.From(request.LanguageCode);
        var cacheKey = CacheKeys.LocaleConfig(lang.Value);

        var cached = await _cache.GetAsync<LocaleConfigurationDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var config = await _repository.GetByLanguageAsync(lang, cancellationToken)
            ?? throw new LocaleConfigurationNotFoundException(lang.Value);

        var dto = MapToDto(config);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(6), cancellationToken);

        return dto;
    }

    internal static LocaleConfigurationDto MapToDto(LocaleConfiguration c) =>
        new(c.LanguageCode.Value,
            c.DateFormat,
            c.ShortDateFormat,
            c.TimeFormat,
            c.DateTimeFormat,
            c.FirstDayOfWeek.ToString(),
            c.DecimalSeparator.ToString(),
            c.ThousandsSeparator.ToString(),
            c.DecimalPlaces,
            c.CurrencyDecimalPlaces);
}

// ═══════════════════════════════════════════════════════════════════════════════
// GET ALL LOCALE CONFIGURATIONS
// ═══════════════════════════════════════════════════════════════════════════════

public record GetAllLocaleConfigurationsQuery : IRequest<IReadOnlyList<LocaleConfigurationDto>>;

public sealed class GetAllLocaleConfigurationsQueryHandler
    : IRequestHandler<GetAllLocaleConfigurationsQuery, IReadOnlyList<LocaleConfigurationDto>>
{
    private readonly ILocaleConfigurationRepository _repository;

    public GetAllLocaleConfigurationsQueryHandler(ILocaleConfigurationRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<LocaleConfigurationDto>> Handle(
        GetAllLocaleConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        var configs = await _repository.GetAllAsync(cancellationToken);
        return configs.Select(GetLocaleConfigurationQueryHandler.MapToDto).ToList();
    }
}

namespace ATLAS.i18n.Application.Locales.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// UPSERT LOCALE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════

public record UpsertLocaleConfigurationCommand(
    string LanguageCode,
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int    DecimalPlaces         = 2,
    int    CurrencyDecimalPlaces = 2)
    : IRequest<LocaleConfigurationDto>;

public sealed class UpsertLocaleConfigurationCommandValidator
    : AbstractValidator<UpsertLocaleConfigurationCommand>
{
    public UpsertLocaleConfigurationCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().Length(2).Matches("^[a-zA-Z]{2}$");
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
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 10);
        RuleFor(x => x.CurrencyDecimalPlaces).InclusiveBetween(0, 10);
        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different characters.");
    }
}

public sealed class UpsertLocaleConfigurationCommandHandler
    : IRequestHandler<UpsertLocaleConfigurationCommand, LocaleConfigurationDto>
{
    private readonly ILocaleConfigurationRepository _repository;
    private readonly ILanguageRepository            _languageRepo;
    private readonly IUnitOfWork                    _unitOfWork;
    private readonly ICacheService                  _cache;

    public UpsertLocaleConfigurationCommandHandler(
        ILocaleConfigurationRepository repository,
        ILanguageRepository languageRepo,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<LocaleConfigurationDto> Handle(
        UpsertLocaleConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var lang = LanguageCode.From(request.LanguageCode);

        // Language must exist
        _ = await _languageRepo.GetByCodeAsync(lang, cancellationToken)
            ?? throw new LanguageNotFoundException(lang.Value);

        var firstDay = Enum.Parse<FirstDayOfWeek>(request.FirstDayOfWeek, true);
        var decSep   = request.DecimalSeparator[0];
        var thouSep  = request.ThousandsSeparator[0];

        var existing = await _repository.GetByLanguageAsync(lang, cancellationToken);

        LocaleConfiguration config;
        if (existing is null)
        {
            config = LocaleConfiguration.Create(
                request.LanguageCode, request.DateFormat, request.ShortDateFormat,
                request.TimeFormat, request.DateTimeFormat, firstDay,
                decSep, thouSep, request.DecimalPlaces, request.CurrencyDecimalPlaces);

            _repository.Add(config);
        }
        else
        {
            existing.Update(
                request.DateFormat, request.ShortDateFormat, request.TimeFormat,
                request.DateTimeFormat, firstDay, decSep, thouSep,
                request.DecimalPlaces, request.CurrencyDecimalPlaces);

            _repository.Update(existing);
            config = existing;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKeys.LocaleConfig(lang.Value), cancellationToken);

        return Queries.GetLocaleConfigurationQueryHandler.MapToDto(config);
    }
}

namespace ATLAS.i18n.Application.Currencies.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
// GET CURRENCY FORMATS
// ═══════════════════════════════════════════════════════════════════════════════

public record GetCurrencyFormatsQuery(string LanguageCode)
    : IRequest<IReadOnlyList<CurrencyFormatDto>>;

public sealed class GetCurrencyFormatsQueryHandler
    : IRequestHandler<GetCurrencyFormatsQuery, IReadOnlyList<CurrencyFormatDto>>
{
    private readonly ICurrencyFormatRepository _repository;
    private readonly ICacheService             _cache;

    public GetCurrencyFormatsQueryHandler(
        ICurrencyFormatRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache      = cache;
    }

    public async Task<IReadOnlyList<CurrencyFormatDto>> Handle(
        GetCurrencyFormatsQuery request,
        CancellationToken cancellationToken)
    {
        var lang     = LanguageCode.From(request.LanguageCode);
        var cacheKey = CacheKeys.CurrencyFormatsForLanguage(lang.Value);

        var cached = await _cache.GetAsync<List<CurrencyFormatDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var formats = await _repository.GetByLanguageAsync(lang, cancellationToken);
        var dtos    = formats.Select(MapToDto).ToList();

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromHours(6), cancellationToken);

        return dtos;
    }

    public record GetCurrencyFormatQuery(string LanguageCode, string CurrencyCode)
        : IRequest<CurrencyFormatDto>;

    internal static CurrencyFormatDto MapToDto(CurrencyFormat f) =>
        new(f.Id,
            f.LanguageCode.Value,
            f.CurrencyCode,
            f.CurrencyName,
            f.Symbol,
            f.SymbolPosition.ToString(),
            f.SpaceBetweenSymbolAndAmount,
            f.DecimalSeparator.ToString(),
            f.ThousandsSeparator.ToString(),
            f.DecimalPlaces,
            f.NegativePattern,
            f.Format(1234.56m));
}

namespace ATLAS.i18n.Application.Currencies.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// UPSERT CURRENCY FORMAT
// ═══════════════════════════════════════════════════════════════════════════════

public record UpsertCurrencyFormatCommand(
    string LanguageCode,
    string CurrencyCode,
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool   SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int    DecimalPlaces,
    string NegativePattern = "-{symbol}{amount}")
    : IRequest<CurrencyFormatDto>;

public sealed class UpsertCurrencyFormatCommandValidator
    : AbstractValidator<UpsertCurrencyFormatCommand>
{
    public UpsertCurrencyFormatCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().Length(2).Matches("^[a-zA-Z]{2}$");
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3).Matches("^[a-zA-Z]{3}$")
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
        RuleFor(x => x.NegativePattern).NotEmpty().Must(p => p.Contains("{amount}"))
            .WithMessage("NegativePattern must contain the {amount} placeholder.");
        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different.");
    }
}

public sealed class UpsertCurrencyFormatCommandHandler
    : IRequestHandler<UpsertCurrencyFormatCommand, CurrencyFormatDto>
{
    private readonly ICurrencyFormatRepository _repository;
    private readonly ILanguageRepository       _languageRepo;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ICacheService             _cache;

    public UpsertCurrencyFormatCommandHandler(
        ICurrencyFormatRepository repository,
        ILanguageRepository languageRepo,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository   = repository;
        _languageRepo = languageRepo;
        _unitOfWork   = unitOfWork;
        _cache        = cache;
    }

    public async Task<CurrencyFormatDto> Handle(
        UpsertCurrencyFormatCommand request,
        CancellationToken cancellationToken)
    {
        var lang     = LanguageCode.From(request.LanguageCode);
        var currency = request.CurrencyCode.ToUpperInvariant();

        _ = await _languageRepo.GetByCodeAsync(lang, cancellationToken)
            ?? throw new LanguageNotFoundException(lang.Value);

        var symPos   = Enum.Parse<CurrencySymbolPosition>(request.SymbolPosition, true);
        var decSep   = request.DecimalSeparator[0];
        var thouSep  = request.ThousandsSeparator[0];

        var existing = await _repository.FindAsync(lang, currency, cancellationToken);

        CurrencyFormat format;
        if (existing is null)
        {
            format = CurrencyFormat.Create(
                request.LanguageCode, currency, request.CurrencyName,
                request.Symbol, symPos, request.SpaceBetweenSymbolAndAmount,
                decSep, thouSep, request.DecimalPlaces, request.NegativePattern);

            _repository.Add(format);
        }
        else
        {
            existing.Update(
                request.CurrencyName, request.Symbol, symPos,
                request.SpaceBetweenSymbolAndAmount, decSep, thouSep,
                request.DecimalPlaces, request.NegativePattern);

            _repository.Update(existing);
            format = existing;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            CacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await _cache.RemoveAsync(
            CacheKeys.CurrencyFormatsForLanguage(lang.Value), cancellationToken);

        return Queries.GetCurrencyFormatsQueryHandler.MapToDto(format);
    }
}

public record DeleteCurrencyFormatCommand(string LanguageCode, string CurrencyCode)
    : IRequest<Unit>;

public sealed class DeleteCurrencyFormatCommandHandler
    : IRequestHandler<DeleteCurrencyFormatCommand, Unit>
{
    private readonly ICurrencyFormatRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ICacheService             _cache;

    public DeleteCurrencyFormatCommandHandler(
        ICurrencyFormatRepository repository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache      = cache;
    }

    public async Task<Unit> Handle(
        DeleteCurrencyFormatCommand request,
        CancellationToken cancellationToken)
    {
        var lang     = LanguageCode.From(request.LanguageCode);
        var currency = request.CurrencyCode.ToUpperInvariant();

        var format = await _repository.FindAsync(lang, currency, cancellationToken)
            ?? throw new I18nDomainException(
                $"Currency format '{currency}' for language '{lang.Value}' not found.");

        _repository.Remove(format);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.CurrencyFormatsForLanguage(lang.Value), cancellationToken);

        return Unit.Value;
    }
}
