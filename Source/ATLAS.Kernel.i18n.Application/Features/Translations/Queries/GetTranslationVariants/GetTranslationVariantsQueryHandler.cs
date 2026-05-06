using ATLAS.Kernel.Extensions;
using ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;

/// <summary>
/// 
/// </summary>
/// <param name="repository"></param>
public sealed class GetTranslationVariantsQueryHandler(ITranslationRepository repository) : IRequestHandler<GetTranslationVariantsQuery, Result<IReadOnlyList<TranslationDto>>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<IReadOnlyList<TranslationDto>>> Handle(GetTranslationVariantsQuery request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var items = await repository.GetVariantsAsync(codeResult.Value, cancellationToken);
        return items.Select(GetTranslationsPagedQueryHandler.MapToDto)
                    .ToList()
                    .AsReadOnly()
                    .ToResult();
    }
}
