using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Services;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="lookupService"></param>
/// <param name="cache"></param>
public sealed class GetTranslationQueryHandler(ITranslationLookupService lookupService, ICacheService cache) : IRequestHandler<GetTranslationQuery, Result<TranslationLookupDto>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TranslationLookupDto>> Handle(GetTranslationQuery request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;
        var key = I18nCacheKeys.Translation(code.Value, lang.Value);

        // Use SharedKernel's GetOrCreate — handles cache-aside in one call
        var dto = await cache.GetOrCreateAsync<TranslationLookupDto?>(
            key,
            async ct =>
            {
                var resolved = await lookupService.ResolveAsync(code, lang, ct);
                if (resolved.IsFailure) return null;

                var (text, actualLang, isFallback) = resolved.Value;
                return new TranslationLookupDto(
                    code.Value, lang.Value, text, isFallback, actualLang);
            },
            expiry: TimeSpan.FromHours(1),
            cancellationToken: cancellationToken);

        if (dto is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"No translation found for '{code.Value}' in language '{lang.Value}'.");

        return dto;
    }
}
