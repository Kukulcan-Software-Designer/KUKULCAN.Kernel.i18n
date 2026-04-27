namespace ATLAS.i18n.Infrastructure.Services;

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
    /// <summary>Sentinel: no tenant. EF Core tenant filter is not applied to any i18n entity.</summary>
    public Guid   TenantId            => Guid.Empty;
    public string TenantCode          => "SYSTEM";
    public string Locale              => "en-US";
    public string TimeZoneId          => "UTC";
    public string DefaultCurrencyCode => "USD";
    /// <summary><c>true</c> so that <c>TenantBehavior</c> passes through i18n requests.</summary>
    public bool   IsResolved          => true;
}

/// <summary>
/// HTTP-context-aware <see cref="ICurrentUser"/> for the ATLAS.i18n API.
/// Reads standard JWT claims injected by ASP.NET Core authentication middleware.
/// Falls back gracefully when no user is authenticated (background jobs, seeding).
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private System.Security.Claims.ClaimsPrincipal? User
        => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated
        => User?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string UserName
        => User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? User?.FindFirst("preferred_username")?.Value
        ?? "system";

    public string? Email
        => User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public Guid TenantId => Guid.Empty; // i18n is global

    public IReadOnlyList<string> Roles
        => User?.FindAll(System.Security.Claims.ClaimTypes.Role)
               .Select(c => c.Value)
               .ToList()
           ?? [];

    public bool IsInRole(string role)    => User?.IsInRole(role) ?? false;
    public bool IsInAllRoles(params string[] roles) => roles.All(IsInRole);
}

/// <summary>
/// System clock abstraction. Injects the real UTC clock in production.
/// Swap this registration in tests to control time deterministically.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
