using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;

/// <summary>
/// 
/// </summary>
/// <param name="repository"></param>
/// <param name="unitOfWork"></param>
public sealed class SetTranslationReviewedCommandHandler(ITranslationRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<SetTranslationReviewedCommand, Result>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result> Handle(SetTranslationReviewedCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var translation = await repository.FindAsync(
            codeResult.Value, langResult.Value, cancellationToken);

        if (translation is null)
            return Error.NotFound(
                "Translation.NotFound",
                $"Translation '{codeResult.Value.Value}' for language '{langResult.Value.Value}' was not found.");

        if (request.IsReviewed) translation.MarkAsReviewed();
        else translation.MarkAsUnreviewed();

        repository.Update(translation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
