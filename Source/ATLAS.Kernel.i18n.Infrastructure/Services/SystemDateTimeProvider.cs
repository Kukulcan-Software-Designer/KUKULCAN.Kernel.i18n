namespace ATLAS.Kernel.i18n.Infrastructure.Services;

/// <summary>
/// System clock abstraction. Injects the real UTC clock in production.
/// Swap this registration in tests to control time deterministically.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
