using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="repository"></param>
/// <param name="unitOfWork"></param>
/// <param name="cache"></param>
public sealed class UpdateTranslationCommandHandler(ITranslationRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<UpdateTranslationCommand, Result<TranslationDto>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TranslationDto>> Handle(UpdateTranslationCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;
        var translation = await repository.FindAsync(code, lang, cancellationToken);

        if (translation is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"Translation '{code.Value}' for language '{lang.Value}' was not found.");

        var updateResult = translation.UpdateText(request.NewText);
        if (updateResult.IsFailure) return updateResult.Error;

        translation.UpdateContext(request.NewContext);

        repository.Update(translation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(I18nCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return CreateTranslationCommandHandler.MapToDto(translation);
    }
}
