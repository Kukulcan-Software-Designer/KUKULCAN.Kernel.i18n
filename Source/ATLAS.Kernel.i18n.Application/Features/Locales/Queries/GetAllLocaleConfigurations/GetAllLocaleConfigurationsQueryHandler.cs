using ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetAllLocaleConfigurations;

/// <summary>
/// Handles queries to retrieve all locale configuration records and returns them as data transfer objects.
/// </summary>
/// <param name="repository">The repository used to access locale configuration data.</param>
public sealed class GetAllLocaleConfigurationsQueryHandler(ILocaleConfigurationRepository repository) : IRequestHandler<GetAllLocaleConfigurationsQuery, Result<IReadOnlyList<LocaleConfigurationDto>>>
{
    /// <summary>
    /// Handles the retrieval of all locale configuration records as data transfer objects.º
    /// </summary>
    /// <param name="request">The query object containing any parameters required to retrieve locale configurations.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with a read-only
    /// list of LocaleConfigurationDto instances representing all locale configurations.</returns>
    public async Task<Result<IReadOnlyList<LocaleConfigurationDto>>> Handle(GetAllLocaleConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var configs = await repository.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<LocaleConfigurationDto>>.Ok(
            [.. configs.Select(GetLocaleConfigurationQueryHandler.MapToDto)]);
    }
}
