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
/// <param name="httpContextAccessor">The httpContextAccessor parameter.</param>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    /// <summary>
    /// Gets IsAuthenticated.
    /// </summary>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Provides functionality for this member.
    /// </summary>
    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    /// <summary>
    /// Executes FindFirst.
    /// </summary>
    public string UserName => User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("preferred_username")?.Value
        ?? "system";

    /// <summary>
    /// Executes FindFirst.
    /// </summary>
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst("email")?.Value;

    /// <summary>
    /// Gets TenantId.
    /// </summary>
    public Guid TenantId => Guid.Empty; // i18n is global

    /// <summary>
    /// Executes FindAll.
    /// </summary>
    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value).ToList()
        ?? [];

    /// <summary>
    /// Executes IsInRole.
    /// </summary>
    /// <param name="role">The role parameter.</param>
    /// <returns>The operation result.</returns>
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    /// <summary>
    /// Executes IsInAllRoles.
    /// </summary>
    /// <param name="roles">The roles parameter.</param>
    /// <returns>The operation result.</returns>
    public bool IsInAllRoles(params string[] roles) => roles.All(IsInRole);
}
