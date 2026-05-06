using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;

/// <summary>
/// Handles queries to retrieve currency format definitions for a specified language.
/// </summary>
/// <param name="repository">The repository used to access currency format data.</param>
public sealed class GetCurrencyFormatsQueryHandler(ICurrencyFormatRepository repository) : IRequestHandler<GetCurrencyFormatsQuery, Result<IReadOnlyList<CurrencyFormatDto>>>
{
    /// <summary>
    /// Handles the retrieval of currency format definitions for a specified language.
    /// </summary>
    /// <param name="request">The query containing the language code for which to retrieve currency formats.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A result containing a read-only list of currency format data transfer objects for the specified language.
    /// Returns an error result if the language code is invalid.</returns>
    public async Task<Result<IReadOnlyList<CurrencyFormatDto>>> Handle(GetCurrencyFormatsQuery request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var formats = await repository.GetByLanguageAsync(langResult.Value, cancellationToken);

        return Result<IReadOnlyList<CurrencyFormatDto>>.Ok(
            [.. formats.Select(MapToDto)]);
    }

    internal static CurrencyFormatDto MapToDto(CurrencyFormat f) =>
        new(f.Id, f.LanguageCode.Value, f.CurrencyCode, f.CurrencyName,
            f.Symbol, f.SymbolPosition.ToString(), f.SpaceBetweenSymbolAndAmount,
            f.DecimalSeparator.ToString(), f.ThousandsSeparator.ToString(),
            f.DecimalPlaces, f.NegativePattern,
            f.Format(1_234.56m),   // pre-formatted example
            f.CreatedAt, f.UpdatedAt);
}
