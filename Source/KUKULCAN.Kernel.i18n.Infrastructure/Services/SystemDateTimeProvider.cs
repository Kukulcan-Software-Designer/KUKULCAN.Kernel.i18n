namespace KUKULCAN.Kernel.i18n.Infrastructure.Services;

/// <summary>
/// System clock abstraction. Injects the real UTC clock in production.
/// Swap this registration in tests to control time deterministically.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Gets UtcNow.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets Today.
    /// </summary>
    public DateOnly Today { get; }

    /// <summary>
    /// Gets UnixTimestampSeconds.
    /// </summary>
    public long UnixTimestampSeconds { get; }
}
