using ATLAS.i18n.Application.Common.Behaviors;
using ATLAS.i18n.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.i18n.Application;

/// <summary>
/// Registers all Application layer services into the DI container.
/// Call from Program.cs or Startup.cs:  services.AddAtlasI18nApplication();
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddAtlasI18nApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceRegistration).Assembly;

        // MediatR — scans the Application assembly for all IRequest handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — auto-register all validators in the Application assembly
        services.AddValidatorsFromAssembly(assembly);

        // MediatR pipeline behaviors (order matters — validation runs before logging)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Domain services
        services.AddScoped<ILanguageDomainService,    LanguageDomainService>();
        services.AddScoped<ITranslationDomainService, TranslationDomainService>();

        return services;
    }
}
