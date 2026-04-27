namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Represents a single translated text entry in the ATLAS platform.
///
/// <para>
/// Each translation is uniquely identified by the combination of:
/// <list type="bullet">
///   <item><see cref="Code"/> — e.g. <c>CRM0001</c>, <c>PIM0042</c>, <c>CORE0001</c>.</item>
///   <item><see cref="LanguageCode"/> — BCP-47 tag, e.g. <c>en-US</c>, <c>es-ES</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Fallback rule:</b> English (<c>en</c>) is the default language. Every text
/// <b>must</b> have an English entry. The application layer resolves the
/// <see cref="LanguageCode.FallbackChain"/> automatically when a translation is not
/// found in the requested language.
/// </para>
///
/// <para>
/// Extends <see cref="AuditableEntityBase{TId}"/> from <c>Atlas.SharedKernel.Domain</c>
/// so all audit fields are populated automatically by <c>AuditSaveChangesInterceptor</c>.
/// Translations are <b>global</b> (not tenant-scoped) and support soft deletion is
/// intentionally <b>not</b> implemented (translations may be removed physically via admin).
/// </para>
/// </summary>
public sealed class Translation : AuditableEntityBase<Guid>
{
    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// Translation code in the format <c>{MODULE}{NNNN}</c>.
    /// The module prefix identifies which ATLAS API owns this string.
    /// </summary>
    public TranslationCode Code { get; private set; } = null!;

    /// <summary>
    /// BCP-47 language tag (e.g. <c>es-ES</c>, <c>en-US</c>).
    /// Stored as a <see cref="LanguageCode"/> value object from Atlas.SharedKernel.
    /// </summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    /// <summary>
    /// The translated text. May contain positional placeholders (<c>{0}</c>, <c>{1}</c>)
    /// that callers replace at runtime. Example: <c>"Welcome, {0}!"</c>
    /// </summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>
    /// Optional translator context — explains where and how this string is used in the UI.
    /// Never shown to end users. Helps translators stay within UI constraints.
    /// </summary>
    public string? Context { get; private set; }

    /// <summary>
    /// Maximum allowed character length for this string in the UI.
    /// <c>null</c> means unrestricted. Enforced on creation and update.
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// <c>true</c> when a human translator has reviewed and approved this text.
    /// Any text update resets this to <c>false</c> automatically.
    /// </summary>
    public bool IsReviewed { get; private set; }

    // ── EF Core constructor ───────────────────────────────────────────────────

    // ReSharper disable once UnusedMember.Local
    private Translation() { }

    // ── Factory method ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new <see cref="Translation"/> entry after validating all inputs.
    /// </summary>
    /// <param name="id">Sequential Guid — use <c>SequentialGuid.NewSequentialGuidAtEnd()</c>.</param>
    /// <param name="rawCode">Translation code string, e.g. <c>"CRM0001"</c>.</param>
    /// <param name="bcp47LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
    /// <param name="text">The translated text. Must not be empty.</param>
    /// <param name="context">Optional translator context note.</param>
    /// <param name="maxLength">Optional maximum character length.</param>
    public static Result<Translation> Create(
        Guid    id,
        string  rawCode,
        string  bcp47LanguageCode,
        string  text,
        string? context   = null,
        int?    maxLength = null)
    {
        var codeResult = TranslationCode.From(rawCode);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(bcp47LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        if (string.IsNullOrWhiteSpace(text))
            return Error.Validation("Translation.Text.Empty", "Translation text must not be empty.");

        if (maxLength.HasValue && maxLength.Value < 1)
            return Error.Validation("Translation.MaxLength.Invalid", "MaxLength must be a positive integer.");

        var trimmedText = text.Trim();
        if (maxLength.HasValue && trimmedText.Length > maxLength.Value)
            return Error.Validation(
                "Translation.Text.ExceedsMaxLength",
                $"Text length ({trimmedText.Length}) exceeds MaxLength ({maxLength.Value}).");

        return new Translation
        {
            Id           = Guard.Against.Default(id, nameof(id)),
            Code         = codeResult.Value,
            LanguageCode = langResult.Value,
            Text         = trimmedText,
            Context      = context?.Trim(),
            MaxLength    = maxLength,
            IsReviewed   = false,
        };
    }

    // ── Business methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the translation text. Automatically resets <see cref="IsReviewed"/> to
    /// <c>false</c> since the new text requires re-approval.
    /// </summary>
    public Result UpdateText(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
            return Error.Validation("Translation.Text.Empty", "Translation text must not be empty.");

        var trimmed = newText.Trim();

        if (MaxLength.HasValue && trimmed.Length > MaxLength.Value)
            return Error.Validation(
                "Translation.Text.ExceedsMaxLength",
                $"Text length ({trimmed.Length}) exceeds MaxLength ({MaxLength.Value}).");

        Text       = trimmed;
        IsReviewed = false;  // any change requires re-review
        return Result.Ok();
    }

    /// <summary>Updates the translator context note.</summary>
    public void UpdateContext(string? context)
        => Context = context?.Trim();

    /// <summary>
    /// Changes or removes the maximum length constraint.
    /// Fails if the new limit is smaller than the current text length.
    /// </summary>
    public Result SetMaxLength(int? maxLength)
    {
        if (maxLength.HasValue)
        {
            if (maxLength.Value < 1)
                return Error.Validation("Translation.MaxLength.Invalid", "MaxLength must be a positive integer.");

            if (Text.Length > maxLength.Value)
                return Error.Validation(
                    "Translation.MaxLength.TooSmall",
                    $"Cannot set MaxLength to {maxLength.Value}: " +
                    $"current text length is {Text.Length}.");
        }

        MaxLength = maxLength;
        return Result.Ok();
    }

    /// <summary>Marks this translation as reviewed by a human translator.</summary>
    public void MarkAsReviewed() => IsReviewed = true;

    /// <summary>Removes the reviewed status (e.g. after a source-language update).</summary>
    public void MarkAsUnreviewed() => IsReviewed = false;
}
