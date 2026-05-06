using ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;
using ATLAS.Kernel.Infrastructure.Primitives;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;

/// <summary>
/// Handles commands to create or update locale configuration settings for a specific language.
/// </summary>
/// <remarks>This handler ensures that locale configuration is created if it does not exist, or updated if it
/// does. It validates the language code and ensures the associated language exists before performing operations. Cache
/// entries related to the locale configuration are invalidated after changes are saved.</remarks>
/// <param name="repository">The repository used to access and persist locale configuration entities.</param>
/// <param name="languageRepo">The repository used to verify the existence of languages by code.</param>
/// <param name="unitOfWork">The unit of work used to commit changes to the data store.</param>
/// <param name="cache">The cache service used to invalidate locale configuration cache entries after updates.</param>
public sealed class UpsertLocaleConfigurationCommandHandler(ILocaleConfigurationRepository repository, ILanguageRepository languageRepo,
    IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<UpsertLocaleConfigurationCommand, Result<LocaleConfigurationDto>>
{
    /// <summary>
    /// Creates or updates the locale configuration for a specified language and returns the resulting configuration
    /// data transfer object.
    /// </summary>
    /// <remarks>If the specified language does not exist, the operation returns a not found error. The method
    /// ensures that locale configuration is either created or updated as appropriate for the given language.</remarks>
    /// <param name="request">The command containing the language code and locale configuration details to create or update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A result containing the locale configuration data transfer object if the operation succeeds; otherwise, a result
    /// with an error describing the failure.</returns>
    public async Task<Result<LocaleConfigurationDto>> Handle(UpsertLocaleConfigurationCommand request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang = langResult.Value;

        // Language must exist
        if (!await languageRepo.ExistsByCodeAsync(lang.Value, cancellationToken))
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        var firstDay = Enum.Parse<FirstDayOfWeek>(request.FirstDayOfWeek, true);
        var decSep = request.DecimalSeparator[0];
        var thousSep = request.ThousandsSeparator[0];
        var existing = await repository.GetByLanguageAsync(lang, cancellationToken);

        LocaleConfiguration config;

        if (existing is null)
        {
            var createResult = LocaleConfiguration.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                request.LanguageCode, request.DateFormat, request.ShortDateFormat,
                request.TimeFormat, request.DateTimeFormat, firstDay,
                decSep, thousSep, request.DecimalPlaces, request.CurrencyDecimalPlaces);

            if (createResult.IsFailure) return createResult.Error;

            await repository.AddAsync(createResult.Value, cancellationToken);
            config = createResult.Value;
        }
        else
        {
            var updateResult = existing.Update(
                request.DateFormat, request.ShortDateFormat,
                request.TimeFormat, request.DateTimeFormat, firstDay,
                decSep, thousSep, request.DecimalPlaces, request.CurrencyDecimalPlaces);

            if (updateResult.IsFailure) return updateResult.Error;

            repository.Update(existing);
            config = existing;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.LocaleConfig(lang.Value), cancellationToken);

        return GetLocaleConfigurationQueryHandler.MapToDto(config);
    }
}
