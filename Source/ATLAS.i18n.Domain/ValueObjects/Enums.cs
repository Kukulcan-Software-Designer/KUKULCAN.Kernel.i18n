namespace ATLAS.i18n.Domain.ValueObjects;

/// <summary>Defines where the currency symbol is placed relative to the numeric amount.</summary>
public enum CurrencySymbolPosition
{
    /// <summary>Symbol appears before the amount — e.g. <c>$1,234.56</c> or <c>€1.234,56</c>.</summary>
    Before = 1,

    /// <summary>Symbol appears after the amount — e.g. <c>1.234,56 €</c> or <c>1,234.56 £</c>.</summary>
    After = 2,
}

/// <summary>Defines the first day of the week for calendar display in a locale.</summary>
public enum FirstDayOfWeek
{
    /// <summary>Week starts on Sunday (en-US convention).</summary>
    Sunday = 0,

    /// <summary>Week starts on Monday (most of Europe and Latin America).</summary>
    Monday = 1,

    /// <summary>Week starts on Saturday (some Middle Eastern locales).</summary>
    Saturday = 6,
}
