namespace ATLAS.i18n.Domain.ValueObjects;

/// <summary>
/// Unique translation identifier with the format <c>{MODULE}{NNNN}</c>.
/// <list type="table">
///   <item><term>MODULE</term><description>2–5 uppercase letters identifying the ATLAS API module (CRM, PIM, WMS, AUTH, CORE…).</description></item>
///   <item><term>NNNN</term> <description>4-digit zero-padded sequential number (0001–9999).</description></item>
/// </list>
/// Examples: <c>CRM0001</c>, <c>PIM0042</c>, <c>WMS9999</c>, <c>AUTH0010</c>, <c>CORE0001</c>.
/// </summary>
/// <remarks>
/// Extends <see cref="ValueObject"/> from <c>Atlas.SharedKernel.Domain</c> so equality
/// is value-based and EF Core stores it via a value conversion.
/// Creation returns <see cref="Result{T}"/> — no exceptions for business-rule violations.
/// </remarks>
public sealed class TranslationCode : ValueObject
{
    /// <summary>Number of digits in the sequential part.</summary>
    public const int NumericLength = 4;

    /// <summary>Minimum letters allowed in the module prefix.</summary>
    public const int MinModuleLength = 2;

    /// <summary>Maximum letters allowed in the module prefix.</summary>
    public const int MaxModuleLength = 5;

    /// <summary>Full code string (e.g. <c>"CRM0001"</c>).</summary>
    public string Value { get; }

    /// <summary>Uppercase API module prefix (e.g. <c>"CRM"</c>).</summary>
    public string Module { get; }

    /// <summary>Numeric sequence within the module (1–9999).</summary>
    public int Sequence { get; }

    private TranslationCode(string value, string module, int sequence)
    {
        Value    = value;
        Module   = module;
        Sequence = sequence;
    }

    // ── Factory methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Parses a raw string such as <c>"CRM0001"</c> and returns a
    /// <see cref="Result{TranslationCode}"/> describing success or the validation failure.
    /// </summary>
    public static Result<TranslationCode> From(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Error.Validation(
                "TranslationCode.Empty",
                "Translation code must not be empty.");

        var normalised = raw.Trim().ToUpperInvariant();

        if (normalised.Length < MinModuleLength + NumericLength)
            return Error.Validation(
                "TranslationCode.TooShort",
                $"'{raw}' is too short. Expected MODULE (2–5 letters) + 4 digits, e.g. 'CRM0001'.");

        var numericPart = normalised[^NumericLength..];
        var modulePart  = normalised[..^NumericLength];

        if (!numericPart.All(char.IsDigit))
            return Error.Validation(
                "TranslationCode.InvalidNumericPart",
                $"'{raw}' must end with exactly {NumericLength} digits.");

        if (modulePart.Length < MinModuleLength || modulePart.Length > MaxModuleLength)
            return Error.Validation(
                "TranslationCode.InvalidModuleLength",
                $"Module prefix '{modulePart}' must be between {MinModuleLength} and {MaxModuleLength} letters.");

        if (!modulePart.All(char.IsLetter))
            return Error.Validation(
                "TranslationCode.InvalidModuleChars",
                $"Module prefix '{modulePart}' must contain only letters.");

        var sequence = int.Parse(numericPart);
        if (sequence < 1)
            return Error.Validation(
                "TranslationCode.SequenceZero",
                $"Sequence number in '{raw}' must be ≥ 1 (use 0001–9999).");

        return new TranslationCode(normalised, modulePart, sequence);
    }

    /// <summary>
    /// Creates a <see cref="TranslationCode"/> from separate module and sequence components.
    /// </summary>
    public static Result<TranslationCode> Create(string module, int sequence)
    {
        if (string.IsNullOrWhiteSpace(module))
            return Error.Validation("TranslationCode.ModuleEmpty", "Module prefix must not be empty.");

        var normalised = module.Trim().ToUpperInvariant();

        if (normalised.Length < MinModuleLength || normalised.Length > MaxModuleLength)
            return Error.Validation(
                "TranslationCode.InvalidModuleLength",
                $"Module prefix '{module}' must be between {MinModuleLength} and {MaxModuleLength} letters.");

        if (!normalised.All(char.IsLetter))
            return Error.Validation(
                "TranslationCode.InvalidModuleChars",
                $"Module prefix '{module}' must contain only letters.");

        if (sequence is < 1 or > 9999)
            return Error.Validation(
                "TranslationCode.SequenceOutOfRange",
                $"Sequence must be between 1 and 9999. Got: {sequence}.");

        return new TranslationCode($"{normalised}{sequence:D4}", normalised, sequence);
    }

    // ── ValueObject ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Implicitly converts to its string representation.</summary>
    public static implicit operator string(TranslationCode code) => code.Value;
}
