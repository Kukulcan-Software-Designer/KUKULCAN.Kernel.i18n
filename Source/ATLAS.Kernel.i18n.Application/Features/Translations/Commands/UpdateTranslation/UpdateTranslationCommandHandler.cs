using ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

/// <summary>
/// Represents the UpdateTranslationCommandHandler type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
/// <param name="unitOfWork">The unitOfWork parameter.</param>
/// <param name="cache">The cache parameter.</param>
public sealed class UpdateTranslationCommandHandler(ITranslationRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<UpdateTranslationCommand, Result<TranslationDto>>
{
    /// <summary>
    /// Handles the update of a translation entity.
    /// </summary>
    /// <param name="request">The command containing the details of the translation to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    public async Task<Result<TranslationDto>> Handle(UpdateTranslationCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure)
            return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;
        var translation = await repository.FindAsync(code, lang, cancellationToken);

        if (translation is null)
            return Error.NotFound("Translation.NotFound", $"Translation '{code.Value}' for language '{lang.Value}' was not found.");

        var updateResult = translation.UpdateText(request.NewText);
        if (updateResult.IsFailure)
            return updateResult.Error;
        translation.UpdateContext(request.NewContext);
        repository.Update(translation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return CreateTranslationCommandHandler.MapToDto(translation);
    }
}
