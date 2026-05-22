using ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;

/// <summary>
/// Handles queries to retrieve all locale configuration records and returns them as data transfer objects.
/// </summary>
/// <param name="repository">The repository used to access locale configuration data.</param>
public sealed class GetAllLocaleConfigurationsQueryHandler(ILocaleConfigurationRepository repository)
        : IRequestHandler<GetAllLocaleConfigurationsQuery, Result<IReadOnlyList<LocaleConfigurationDto>>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<IReadOnlyList<LocaleConfigurationDto>>> Handle(GetAllLocaleConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var configs = await repository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<LocaleConfigurationDto>>.Ok([.. configs.Select(GetLocaleConfigurationQueryHandler.MapToDto)]);
    }
}
