using ATLAS.Kernel.Extensions;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;

/// <summary>
/// Represents the GetTranslationVariantsQueryHandler type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
public sealed class GetTranslationVariantsQueryHandler(ITranslationRepository repository) : IRequestHandler<GetTranslationVariantsQuery, Result<IReadOnlyList<TranslationDto>>>
{
    /// <summary>
    /// Handles the retrieval of translation variants for a specific translation code.
    /// </summary>
    /// <param name="request">The query containing the details of the translation code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the query, containing the list of translation variants.</returns>
    public async Task<Result<IReadOnlyList<TranslationDto>>> Handle(GetTranslationVariantsQuery request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure)
            return codeResult.Error;

        var items = await repository.GetVariantsAsync(codeResult.Value, cancellationToken);
        return items.Select(GetTranslationsPagedQueryHandler.MapToDto)
                    .ToList()
                    .AsReadOnly()
                    .ToResult();
    }
}
