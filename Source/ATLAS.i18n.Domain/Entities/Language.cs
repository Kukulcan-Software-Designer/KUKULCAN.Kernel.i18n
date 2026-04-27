namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Represents a language supported by the ATLAS platform.
///
/// <para>
/// <b>Hierarchy:</b> extends <see cref="MasterEntity{TId}"/> from
/// <c>Atlas.SharedKernel.Domain</c>, which provides:
/// <list type="bullet">
///   <item>Strongly-typed <c>Guid</c> primary key (<see cref="EntityBase{TId}.Id"/>).</item>
///   <item>Full audit trail (<c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>, <c>UpdatedBy</c>)
///         populated automatically by <c>AuditSaveChangesInterceptor</c>.</item>
///   <item><c>IsActive</c> / <c>Activate()</c> / <c>Deactivate()</c> for lifecycle management.</item>
///   <item><see cref="IMasterData"/> marker — repository layer never exposes a Delete method.</item>
/// </list>
/// </para>
///
/// <para>
/// Languages are <b>global</b> (not tenant-scoped) and are never physically deleted.
/// The English language (<c>en-US</c>) is always the default fallback language.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var result = Language.Create(
///     Guid.NewGuid(), "es-ES", "Spanish", "Español", isDefault: false);
///
/// if (result.IsFailure)
///     return result.Error;
///
/// var lang = result.Value;
/// lang.SetLocaleConfiguration(localeConfig);
/// </code>
/// </example>
public sealed class Language : MasterEntity<Guid>
{
    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// BCP-47 language tag, normalised to lowercase-UPPERCASE (e.g. <c>es-ES</c>, <c>en-US</c>).
    /// Unique within the system. Used as the business identifier for lookups.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>English name of the language (e.g. <c>"Spanish"</c>, <c>"French"</c>).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Native name of the language (e.g. <c>"Español"</c>, <c>"Français"</c>).</summary>
    public string NativeName { get; private set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, this is the fallback language for all translation lookups.
    /// Exactly one language must be the default at any time. Defaults to English (<c>en-US</c>).
    /// </summary>
    public bool IsDefault { get; private set; }

    // ── Navigation properties (owned within this aggregate) ───────────────────

    private LocaleConfiguration? _localeConfiguration;
    /// <summary>Locale formatting rules for this language (dates, numbers, separators).</summary>
    public LocaleConfiguration? LocaleConfiguration => _localeConfiguration;

    private readonly List<CurrencyFormat> _currencyFormats = [];
    /// <summary>Currency formatting rules for this language.</summary>
    public IReadOnlyList<CurrencyFormat> CurrencyFormats => _currencyFormats.AsReadOnly();

    // ── EF Core constructor ───────────────────────────────────────────────────

    // ReSharper disable once UnusedMember.Local
    private Language() { }

    // ── Factory method ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="Language"/> with the given BCP-47 code and display names.
    /// </summary>
    /// <param name="id">Sequential Guid — use <c>SequentialGuid.NewSequentialGuidAtEnd()</c>.</param>
    /// <param name="bcp47Code">
    /// Full BCP-47 language tag (e.g. <c>"es-ES"</c>, <c>"en-US"</c>, <c>"ca-ES"</c>).
    /// Validated by <see cref="LanguageCode.Create"/>.
    /// </param>
    /// <param name="name">English display name (e.g. <c>"Spanish"</c>).</param>
    /// <param name="nativeName">Native display name (e.g. <c>"Español"</c>).</param>
    /// <param name="isDefault"><c>true</c> if this language is the global fallback.</param>
    public static Result<Language> Create(
        Guid   id,
        string bcp47Code,
        string name,
        string nativeName,
        bool   isDefault = false)
    {
        var codeResult = LanguageCode.Create(bcp47Code);
        if (codeResult.IsFailure) return codeResult.Error;

        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Language.Name.Empty", "Language name must not be empty.");

        if (string.IsNullOrWhiteSpace(nativeName))
            return Error.Validation("Language.NativeName.Empty", "Native name must not be empty.");

        return new Language
        {
            Id         = Guard.Against.Default(id, nameof(id)),
            Code       = codeResult.Value.Value,
            Name       = name.Trim(),
            NativeName = nativeName.Trim(),
            IsDefault  = isDefault,
        };
    }

    // ── Business methods ──────────────────────────────────────────────────────

    /// <summary>Updates the display names and optionally the BCP-47 code for this language.</summary>
    public Result Update(string name, string nativeName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Language.Name.Empty", "Language name must not be empty.");
        if (string.IsNullOrWhiteSpace(nativeName))
            return Error.Validation("Language.NativeName.Empty", "Native name must not be empty.");

        Name       = name.Trim();
        NativeName = nativeName.Trim();
        return Result.Ok();
    }

    /// <summary>
    /// Attempts to deactivate this language.
    /// Returns <see cref="Error.Conflict"/> when called on the default language.
    /// </summary>
    public new Result Deactivate()
    {
        if (IsDefault)
            return Error.Conflict(
                "Language.Default.CannotDeactivate",
                $"Language '{Code}' is the platform default and cannot be deactivated. " +
                "Transfer the default to another language first.");

        base.Deactivate();
        return Result.Ok();
    }

    /// <summary>
    /// Marks this language as the platform default.
    /// Called by the domain service when transferring the default designation.
    /// </summary>
    internal void MarkAsDefault()
    {
        IsDefault = true;
        base.Activate(); // default language must always be active
    }

    /// <summary>
    /// Removes the default flag.
    /// Called by the domain service when transferring the default to another language.
    /// </summary>
    internal void UnmarkDefault()
        => IsDefault = false;

    /// <summary>
    /// Attaches or replaces the locale configuration for this language.
    /// </summary>
    public void SetLocaleConfiguration(LocaleConfiguration configuration)
    {
        Guard.Against.Null(configuration, nameof(configuration));
        _localeConfiguration = configuration;
    }

    /// <summary>Adds a currency format. Fails if the same ISO 4217 code already exists.</summary>
    public Result AddCurrencyFormat(CurrencyFormat format)
    {
        Guard.Against.Null(format, nameof(format));

        if (_currencyFormats.Any(c => c.CurrencyCode == format.CurrencyCode))
            return Error.Conflict(
                "Language.CurrencyFormat.Duplicate",
                $"A currency format for '{format.CurrencyCode}' already exists in language '{Code}'.");

        _currencyFormats.Add(format);
        return Result.Ok();
    }

    /// <summary>Removes the currency format for the given ISO 4217 code.</summary>
    public Result RemoveCurrencyFormat(string currencyCode)
    {
        var format = _currencyFormats.FirstOrDefault(
            c => c.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));

        if (format is null)
            return Error.NotFound(
                "Language.CurrencyFormat.NotFound",
                $"No currency format for '{currencyCode}' found in language '{Code}'.");

        _currencyFormats.Remove(format);
        return Result.Ok();
    }
}
