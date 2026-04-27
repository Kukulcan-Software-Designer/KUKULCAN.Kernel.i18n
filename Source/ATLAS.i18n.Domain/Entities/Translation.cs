using ATLAS.i18n.Domain.SeedWork;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Represents a single translated text entry in the ATLAS platform.
/// 
/// Each translation is uniquely identified by the combination of:
///   - <see cref="Code"/>     → e.g. CRM0001, PIM0042, AUTH0010
///   - <see cref="LanguageCode"/> → e.g. EN, ES
///
/// English (EN) is the default language. Every text MUST have an English entry.
/// Other languages are optional; the system falls back to English when a translation
/// in the requested language is not found.
/// </summary>
public sealed class Translation : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Unique translation code in the format {MODULE}{NNNN}.
    /// The module prefix identifies which ATLAS API owns this text.
    /// Example values: CRM0001, PIM0042, WMS0100, AUTH0001, CORE0001.
    /// </summary>
    public TranslationCode Code { get; private set; } = null!;

    /// <summary>ISO 639-1 language code for this translation entry.</summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    /// <summary>
    /// The translated text. May contain simple placeholders in the form {0}, {1}
    /// that callers must replace at runtime. Example: "Welcome, {0}!"
    /// </summary>
    public string Text { get; private set; } = null!;

    /// <summary>
    /// Optional free-text description that explains the context in which this
    /// string appears, to help translators. Never exposed to end users.
    /// </summary>
    public string? Context { get; private set; }

    /// <summary>
    /// Maximum character length allowed for this string in the UI.
    /// Null means no restriction. Helps translators stay within UI bounds.
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// Indicates whether this translation has been reviewed/approved by a human translator.
    /// Unreviewed entries are still served but can be flagged in admin tools.
    /// </summary>
    public bool IsReviewed { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private Translation() { }

    private Translation(
        Guid id,
        TranslationCode code,
        LanguageCode languageCode,
        string text,
        string? context,
        int? maxLength)
    {
        Id           = id;
        Code         = code;
        LanguageCode = languageCode;
        Text         = text;
        Context      = context;
        MaxLength    = maxLength;
        IsReviewed   = false;
        CreatedAt    = DateTime.UtcNow;
        UpdatedAt    = DateTime.UtcNow;

        AddDomainEvent(new TranslationCreatedEvent(id, code.Value, languageCode.Value));
    }

    /// <summary>Factory method — canonical way to create a new Translation.</summary>
    public static Translation Create(
        string code,
        string languageCode,
        string text,
        string? context = null,
        int? maxLength = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        ValidateText(text, maxLength);

        return new Translation(
            Guid.NewGuid(),
            TranslationCode.From(code),
            ValueObjects.LanguageCode.From(languageCode),
            text.Trim(),
            context?.Trim(),
            maxLength);
    }

    public void UpdateText(string newText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newText);
        ValidateText(newText, MaxLength);

        Text       = newText.Trim();
        IsReviewed = false;   // Any text change resets review status
        UpdatedAt  = DateTime.UtcNow;

        AddDomainEvent(new TranslationTextUpdatedEvent(Id, Code.Value, LanguageCode.Value));
    }

    public void UpdateContext(string? context)
    {
        Context   = context?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaxLength(int? maxLength)
    {
        if (maxLength.HasValue && maxLength.Value < 1)
            throw new Exceptions.I18nDomainException(
                "MaxLength must be a positive integer.");

        if (maxLength.HasValue && Text.Length > maxLength.Value)
            throw new Exceptions.I18nDomainException(
                $"Current text length ({Text.Length}) exceeds the new MaxLength ({maxLength.Value}).");

        MaxLength = maxLength;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsReviewed()
    {
        IsReviewed = true;
        UpdatedAt  = DateTime.UtcNow;
    }

    public void MarkAsUnreviewed()
    {
        IsReviewed = false;
        UpdatedAt  = DateTime.UtcNow;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static void ValidateText(string text, int? maxLength)
    {
        if (maxLength.HasValue && text.Trim().Length > maxLength.Value)
            throw new Exceptions.I18nDomainException(
                $"Text length ({text.Trim().Length}) exceeds MaxLength ({maxLength.Value}).");
    }
}

// ─── Domain Events ────────────────────────────────────────────────────────────

public sealed record TranslationCreatedEvent(
    Guid TranslationId,
    string Code,
    string LanguageCode) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record TranslationTextUpdatedEvent(
    Guid TranslationId,
    string Code,
    string LanguageCode) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
