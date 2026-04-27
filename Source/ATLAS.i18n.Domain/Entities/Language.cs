using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.SeedWork;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Represents a supported language in the ATLAS platform.
/// This is the aggregate root for language-related information.
/// </summary>
public sealed class Language : AggregateRoot<string>, IAuditableEntity
{
    /// <summary>ISO 639-1 language code (e.g. "EN", "ES"). Acts as the primary key.</summary>
    public new LanguageCode Id { get; private set; } = null!;

    /// <summary>English name of the language (e.g. "English", "Spanish").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Native name of the language (e.g. "Español", "Français").</summary>
    public string NativeName { get; private set; } = null!;

    /// <summary>
    /// Indicates this is the default fallback language (English).
    /// Only one language may be the default at a time.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>Whether translations can be requested in this language.</summary>
    public bool IsActive { get; private set; }

    /// <summary>BCP-47 locale tag for .NET CultureInfo (e.g. "en-US", "es-ES").</summary>
    public string CultureTag { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation - owned by Language aggregate
    private LocaleConfiguration? _localeConfiguration;
    public LocaleConfiguration? LocaleConfiguration => _localeConfiguration;

    private readonly List<CurrencyFormat> _currencyFormats = [];
    public IReadOnlyCollection<CurrencyFormat> CurrencyFormats => _currencyFormats.AsReadOnly();

    // EF Core constructor
    private Language() { }

    private Language(
        LanguageCode code,
        string name,
        string nativeName,
        string cultureTag,
        bool isDefault)
    {
        Id          = code;
        Name        = name;
        NativeName  = nativeName;
        CultureTag  = cultureTag;
        IsDefault   = isDefault;
        IsActive    = true;
        CreatedAt   = DateTime.UtcNow;
        UpdatedAt   = DateTime.UtcNow;

        AddDomainEvent(new LanguageCreatedEvent(code.Value, name, isDefault));
    }

    /// <summary>Factory method — the canonical way to create a new Language.</summary>
    public static Language Create(
        string code,
        string name,
        string nativeName,
        string cultureTag,
        bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureTag);

        return new Language(
            LanguageCode.From(code),
            name.Trim(),
            nativeName.Trim(),
            cultureTag.Trim(),
            isDefault);
    }

    public void Update(string name, string nativeName, string cultureTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureTag);

        Name        = name.Trim();
        NativeName  = nativeName.Trim();
        CultureTag  = cultureTag.Trim();
        UpdatedAt   = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive  = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsDefault)
            throw new DefaultLanguageCannotBeDeactivatedException(Id.Value);

        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Marks this language as the global default. Called by the domain service.</summary>
    internal void SetAsDefault()
    {
        IsDefault = true;
        IsActive  = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Removes the default flag. Called by the domain service when transferring default.</summary>
    internal void UnsetDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLocaleConfiguration(LocaleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _localeConfiguration = configuration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCurrencyFormat(CurrencyFormat currencyFormat)
    {
        ArgumentNullException.ThrowIfNull(currencyFormat);

        if (_currencyFormats.Any(c => c.CurrencyCode == currencyFormat.CurrencyCode))
            throw new I18nDomainException(
                $"A currency format for '{currencyFormat.CurrencyCode}' already exists in language '{Id.Value}'.");

        _currencyFormats.Add(currencyFormat);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCurrencyFormat(string currencyCode)
    {
        var format = _currencyFormats.FirstOrDefault(c => c.CurrencyCode == currencyCode)
            ?? throw new I18nDomainException(
                $"Currency format '{currencyCode}' not found in language '{Id.Value}'.");

        _currencyFormats.Remove(format);
        UpdatedAt = DateTime.UtcNow;
    }
}

// ─── Domain Events ────────────────────────────────────────────────────────────

public sealed record LanguageCreatedEvent(
    string Code,
    string Name,
    bool IsDefault) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
