using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;

/// <summary>
/// Handles update commands for language entities, applying changes and ensuring cache consistency.
/// </summary>
/// <remarks>This handler ensures that language updates are persisted and that relevant cache entries are removed
/// to reflect the latest data. It is intended for use within a CQRS and MediatR-based application
/// architecture.</remarks>
/// <param name="repository">The repository used to access and update language entities.</param>
/// <param name="unitOfWork">The unit of work used to persist changes to the data store.</param>
/// <param name="cache">The cache service used to invalidate language-related cache entries after an update.</param>
public sealed class UpdateLanguageCommandHandler(ILanguageRepository repository, IUnitOfWork unitOfWork, ICacheService cache) :
    IRequestHandler<UpdateLanguageCommand, Result<LanguageDto>>
{
    /// <summary>
    /// Handles the update of a language entity.
    /// </summary>
    /// <param name="request">The command containing the details of the language to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    public async Task<Result<LanguageDto>> Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await repository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.");

        var updateResult = language.Update(request.Name, request.NativeName);

        if (updateResult.IsFailure)
            return updateResult.Error;
        repository.Update(language);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.Language(request.Code), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesAll, cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesActive, cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(language);
    }
}
