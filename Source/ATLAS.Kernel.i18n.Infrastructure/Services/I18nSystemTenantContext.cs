namespace ATLAS.Kernel.i18n.Infrastructure.Services;

/// <summary>
/// System-level <see cref="ITenantContext"/> implementation for the ATLAS.i18n service.
///
/// <para>
/// i18n data is <b>global</b> — it does not belong to any single tenant.
/// This implementation returns a sentinel value (<see cref="Guid.Empty"/>) so that:
/// <list type="bullet">
///   <item><see cref="IsResolved"/> = <c>true</c> (the MediatR <c>TenantBehavior</c> allows the request).</item>
///   <item>The EF Core tenant query filter never activates (no i18n entity implements <c>ITenantAware</c>).</item>
/// </list>
/// </para>
/// </summary>
public sealed class I18nSystemTenantContext : ITenantContext
{
    /// <summary>
    /// Sentinel: no tenant. EF Core tenant filter is not applied to any i18n entity.
    /// </summary>
    public Guid TenantId => Guid.Empty;
    /// <summary>
    /// 
    /// </summary>
    public string TenantCode => "SYSTEM";
    /// <summary>
    /// 
    /// </summary>
    public string Locale => "en-US";
    /// <summary>
    /// 
    /// </summary>
    public string TimeZoneId => "UTC";
    /// <summary>
    /// 
    /// </summary>
    public string DefaultCurrencyCode => "USD";
    /// <summary>
    /// <c>true</c> so that <c>TenantBehavior</c> passes through i18n requests.
    /// </summary>
    public bool IsResolved => true;
}

