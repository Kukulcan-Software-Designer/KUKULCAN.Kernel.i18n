using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;

/// <summary>
/// Handles queries to retrieve locale configuration data for a specified language code.
/// </summary>
/// <remarks>This handler processes requests to obtain locale configuration details, returning a result that
/// indicates success or failure. If no configuration is found for the specified language, a not found error is
/// returned. This class is typically used within a CQRS pattern to separate query logic from command
/// operations.</remarks>
/// <param name="repository">The repository used to access locale configuration data.</param>
public sealed class GetLocaleConfigurationQueryHandler(ILocaleConfigurationRepository repository)
        : IRequestHandler<GetLocaleConfigurationQuery, Result<LocaleConfigurationDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<LocaleConfigurationDto>> Handle(GetLocaleConfigurationQuery request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var config = await repository.GetByLanguageAsync(langResult.Value, cancellationToken);

        return config is null
            ? Error.NotFound("LocaleConfig.NotFound", $"No locale configuration found for language '{request.LanguageCode}'.")
            : MapToDto(config);
    }

    internal static LocaleConfigurationDto MapToDto(LocaleConfiguration c) =>
        new(c.LanguageCode.Value, c.DateFormat, c.ShortDateFormat, c.TimeFormat,
            c.DateTimeFormat, c.FirstDayOfWeek.ToString(),
            c.DecimalSeparator.ToString(), c.ThousandsSeparator.ToString(),
            c.DecimalPlaces, c.CurrencyDecimalPlaces,
            c.CreatedAt, c.UpdatedAt);
}
