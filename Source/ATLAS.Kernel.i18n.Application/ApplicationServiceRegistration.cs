using System.Reflection;
using ATLAS.Kernel.Infrastructure.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ATLAS.Kernel.i18n.Domain.Interfaces.Services;

namespace ATLAS.Kernel.i18n.Application;

/// <summary>
/// Registers all Application layer services.
/// Call from <c>Program.cs</c>: <c>services.AddAtlasI18nApplication();</c>
///
/// <para>
/// MediatR pipeline order (innermost to outermost):
/// <list type="number">
///   <item><see cref="TenantBehavior{TRequest,TResponse}"/> — validates tenant context (not used in i18n; registered by host if needed)</item>
///   <item><see cref="ValidationBehavior{TRequest,TResponse}"/> — FluentValidation, returns <c>Result.Fail</c> on violation</item>
///   <item><see cref="CachingBehavior{TRequest,TResponse}"/> — cache-aside for <see cref="ICacheableRequest"/> queries</item>
///   <item><see cref="LoggingBehavior{TRequest,TResponse}"/> — structured request/response logging with timing</item>
/// </list>
/// All four behaviors are provided by <c>Atlas.SharedKernel.Infrastructure</c> — no local copies needed.
/// </para>
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Executes AddAtlasI18nApplication.
    /// </summary>
    /// <param name="services">The services parameter.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddAtlasI18NApplication(this IServiceCollection services)
    {
        Assembly appAssembly = typeof(ApplicationServiceRegistration).Assembly;

        // ── MediatR ────────────────────────────────────────────────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
        // ── FluentValidation ───────────────────────────────────────────────────
        services.AddValidatorsFromAssembly(appAssembly);
        // ── MediatR pipeline behaviors (from SharedKernel.Infrastructure) ──────
        // Order: first registered = outermost wrapper in the pipeline.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // ── Domain services ────────────────────────────────────────────────────
        services.AddScoped<ITranslationLookupService, TranslationLookupService>();
        services.AddScoped<ILanguageDomainService, LanguageDomainService>();

        return services;
    }
}
