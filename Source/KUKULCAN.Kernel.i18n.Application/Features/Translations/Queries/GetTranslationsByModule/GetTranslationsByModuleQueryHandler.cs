using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;

namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;

/// <summary>
/// Represents the GetTranslationsByModuleQueryHandler type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
public sealed class GetTranslationsByModuleQueryHandler(ITranslationRepository repository) : IRequestHandler<GetTranslationsByModuleQuery, Result<TranslationMapDto>>
{
    /// <summary>
    /// Handles the retrieval of translations for a specific module and language.
    /// </summary>
    /// <param name="request">The query containing the details of the module and language.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the query, containing the translation map.</returns>
    public async Task<Result<TranslationMapDto>> Handle(GetTranslationsByModuleQuery request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var lang = langResult.Value;
        var module = request.Module.ToUpperInvariant();
        // Load requested language
        var requested = await repository.GetByModuleAndLanguageAsync(module, lang, cancellationToken);
        var map = requested.ToDictionary(t => t.Code.Value, t => t.Text);

        // Walk the fallback chain and fill gaps for any missing codes
        foreach (var fallbackTag in lang.FallbackChain.Skip(1)) // skip the first (already loaded)
        {
            var fbLangResult = LanguageCode.Create(fallbackTag);
            if (fbLangResult.IsFailure)
                continue;

            var fallback = await repository.GetByModuleAndLanguageAsync(module, fbLangResult.Value, cancellationToken);

            foreach (var t in fallback)
            {
                if (!map.ContainsKey(t.Code.Value))
                    map[t.Code.Value] = t.Text;
            }
        }

        return new TranslationMapDto(lang.Value, module, map);
    }
}
