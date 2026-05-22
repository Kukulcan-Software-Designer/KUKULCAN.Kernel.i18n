using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;

/// <summary>
/// Represents the DeleteTranslationCommandHandler type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
/// <param name="unitOfWork">The unitOfWork parameter.</param>
/// <param name="cache">The cache parameter.</param>
public sealed class DeleteTranslationCommandHandler(ITranslationRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<DeleteTranslationCommand, Result>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result> Handle(DeleteTranslationCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure)
            return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;

        // Protect English (default) entries — they are the fallback for all other languages
        if (lang.Language == "en")
            return Error.Conflict("Translation.English.ProtectedDelete", $"Cannot delete the English translation for '{code.Value}'. " +
                "Remove all other language variants first, then delete the English entry via the admin bulk-delete tool.");

        var translation = await repository.FindAsync(code, lang, cancellationToken);
        if (translation is null)
            return Error.NotFound("Translation.NotFound", $"Translation '{code.Value}' for language '{lang.Value}' was not found.");
        repository.Remove(translation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return Result.Ok();
    }
}
