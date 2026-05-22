namespace ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;

/// <summary>
/// Defines where the currency symbol is placed relative to the numeric amount.
/// </summary>
public enum CurrencySymbolPosition
{
    /// <summary>
    /// Symbol appears before the amount — e.g. <c>$1,234.56</c> or <c>€1.234,56</c>.
    /// </summary>
    Before = 1,

    /// <summary>
    /// Symbol appears after the amount — e.g. <c>1.234,56 €</c> or <c>1,234.56 £</c>.
    /// </summary>
    After = 2,
}
