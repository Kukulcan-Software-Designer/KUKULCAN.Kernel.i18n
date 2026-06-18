namespace KUKULCAN.Kernel.i18n.Domain.ValueObjects.Enums;


/// <summary>
/// Defines the first day of the week for calendar display in a locale.
/// </summary>
public enum FirstDayOfWeek
{
    /// <summary>
    /// Week starts on Sunday (en-US convention).
    /// </summary>
    Sunday = 0,

    /// <summary>
    /// Week starts on Monday (most of Europe and Latin America).
    /// </summary>
    Monday = 1,

    /// <summary>
    /// Week starts on Saturday (some Middle Eastern locales).
    /// </summary>
    Saturday = 6,
}
