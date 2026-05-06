using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;

/// <summary>
/// Representa una consulta para obtener todas las configuraciones regionales disponibles.
/// </summary>
/// <remarks>Utilice esta consulta con un mediador compatible para recuperar una lista de objetos de configuración
/// regional. El resultado contiene una colección de objetos de solo lectura que describen cada configuración regional
/// disponible en el sistema.</remarks>
public record GetAllLocaleConfigurationsQuery : IRequest<Result<IReadOnlyList<LocaleConfigurationDto>>>;
