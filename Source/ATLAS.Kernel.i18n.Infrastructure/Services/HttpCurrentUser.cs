using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ATLAS.Kernel.i18n.Infrastructure.Services;

/// <summary>
/// HTTP-context-aware <see cref="ICurrentUser"/> for the ATLAS.i18n API.
/// Reads standard JWT claims injected by ASP.NET Core authentication middleware.
/// Falls back gracefully when no user is authenticated (background jobs, seeding).
/// </summary>
/// <remarks>
/// 
/// </remarks>
/// <param name="httpContextAccessor"></param>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <summary>
    /// 
    /// </summary>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// 
    /// </summary>
    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public string UserName => User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("preferred_username")?.Value
        ?? "system";

    /// <summary>
    /// 
    /// </summary>
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst("email")?.Value;

    /// <summary>
    /// 
    /// </summary>
    public Guid TenantId => Guid.Empty; // i18n is global

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value).ToList()
        ?? [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="roles"></param>
    /// <returns></returns>
    public bool IsInAllRoles(params string[] roles) => roles.All(IsInRole);
}
