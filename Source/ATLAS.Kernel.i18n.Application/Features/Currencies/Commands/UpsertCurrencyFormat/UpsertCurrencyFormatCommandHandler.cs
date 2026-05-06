using ATLAS.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;
using ATLAS.Kernel.Infrastructure.Primitives;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;

/// <summary>
/// Handles commands to create or update currency format settings for a specific language and currency.
/// </summary>
/// <remarks>This handler ensures that currency format settings are either created or updated as needed, and that
/// related cache entries are invalidated to reflect the latest changes. It validates language and currency codes before
/// performing operations.</remarks>
/// <param name="repository">The repository used to access and persist currency format entities.</param>
/// <param name="languageRepo">The repository used to verify the existence of languages by code.</param>
/// <param name="unitOfWork">The unit of work used to commit changes to the data store as a single transaction.</param>
/// <param name="cache">The cache service used to invalidate currency format cache entries after changes.</param>
public sealed class UpsertCurrencyFormatCommandHandler(ICurrencyFormatRepository repository, ILanguageRepository languageRepo, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<UpsertCurrencyFormatCommand, Result<CurrencyFormatDto>>
{
    /// <summary>
    /// Creates or updates a currency format for the specified language and currency code.
    /// </summary>
    /// <remarks>If a currency format for the specified language and currency does not exist, a new one is
    /// created. If it exists, the existing format is updated with the provided details. The method also updates the
    /// cache to reflect the changes.</remarks>
    /// <param name="request">The command containing the details of the currency format to create or update, including language code, currency
    /// code, symbol, separators, and formatting options.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A result containing the created or updated currency format as a CurrencyFormatDto if successful; otherwise, an
    /// error result describing the failure.</returns>
    public async Task<Result<CurrencyFormatDto>> Handle(UpsertCurrencyFormatCommand request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var lang = langResult.Value;
        var currency = request.CurrencyCode.ToUpperInvariant();

        if (!await languageRepo.ExistsByCodeAsync(lang.Value, cancellationToken))
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        var symPos = Enum.Parse<CurrencySymbolPosition>(request.SymbolPosition, true);
        var existing = await repository.FindAsync(lang, currency, cancellationToken);

        CurrencyFormat format;

        if (existing is null)
        {
            var createResult = CurrencyFormat.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                request.LanguageCode, currency, request.CurrencyName,
                request.Symbol, symPos, request.SpaceBetweenSymbolAndAmount,
                request.DecimalSeparator[0], request.ThousandsSeparator[0],
                request.DecimalPlaces, request.NegativePattern);

            if (createResult.IsFailure) return createResult.Error;

            await repository.AddAsync(createResult.Value, cancellationToken);
            format = createResult.Value;
        }
        else
        {
            var updateResult = existing.Update(
                request.CurrencyName, request.Symbol, symPos,
                request.SpaceBetweenSymbolAndAmount,
                request.DecimalSeparator[0], request.ThousandsSeparator[0],
                request.DecimalPlaces, request.NegativePattern);

            if (updateResult.IsFailure) return updateResult.Error;

            repository.Update(existing);
            format = existing;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(I18nCacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.CurrencyFormats(lang.Value), cancellationToken);

        return GetCurrencyFormatsQueryHandler.MapToDto(format);
    }
}
