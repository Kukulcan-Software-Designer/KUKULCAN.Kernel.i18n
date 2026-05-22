using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;

/// <summary>
/// Handles queries to retrieve currency format definitions for a specified language.
/// </summary>
/// <param name="repository">The repository used to access currency format data.</param>
public sealed class GetCurrencyFormatsQueryHandler(ICurrencyFormatRepository repository)
    : IRequestHandler<GetCurrencyFormatsQuery, Result<IReadOnlyList<CurrencyFormatDto>>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<IReadOnlyList<CurrencyFormatDto>>> Handle(GetCurrencyFormatsQuery request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var formats = await repository.GetByLanguageAsync(langResult.Value, cancellationToken);

        return Result<IReadOnlyList<CurrencyFormatDto>>.Ok([.. formats.Select(MapToDto)]);
    }

    internal static CurrencyFormatDto MapToDto(CurrencyFormat f) =>
        new(f.Id, f.LanguageCode.Value, f.CurrencyCode, f.CurrencyName, f.Symbol, f.SymbolPosition.ToString(), f.SpaceBetweenSymbolAndAmount,
            f.DecimalSeparator.ToString(), f.ThousandsSeparator.ToString(), f.DecimalPlaces, f.NegativePattern, f.Format(1_234.56m), 
            f.CreatedAt, f.UpdatedAt);
}
