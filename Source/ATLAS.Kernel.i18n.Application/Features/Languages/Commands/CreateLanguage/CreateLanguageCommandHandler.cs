using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.Infrastructure.Primitives;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;

/// <summary>
/// Encapsula el manejo de la creación de un nuevo idioma, gestionando la persistencia, la validación de duplicados y la
/// invalidación de cachés relacionados.
/// </summary>
/// <remarks>Esta clase implementa el patrón Command Handler para la creación de idiomas y asegura la coherencia
/// de los datos y la caché. No es segura para subprocesos concurrentes.</remarks>
/// <param name="repository">El repositorio utilizado para acceder y almacenar entidades de idioma.</param>
/// <param name="unitOfWork">La unidad de trabajo responsable de confirmar los cambios en la base de datos.</param>
/// <param name="cache">El servicio de caché empleado para invalidar las entradas de caché de idiomas tras la creación.</param>
public sealed class CreateLanguageCommandHandler(ILanguageRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<CreateLanguageCommand, Result<LanguageDto>>
{
    /// <summary>
    /// Handles the creation of a new language based on the specified command.
    /// </summary>
    /// <remarks>Returns a conflict error if a language with the specified code already exists. The operation
    /// is performed asynchronously and persists changes to the underlying data store.</remarks>
    /// <param name="request">The command containing the details of the language to create. Must not be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A result containing the data transfer object for the newly created language if successful; otherwise, an error
    /// result indicating the reason for failure, such as a duplicate language code.</returns>
    public async Task<Result<LanguageDto>> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByCodeAsync(request.Code, cancellationToken))
            return Error.Conflict(
                "Language.Duplicate",
                $"Language '{request.Code}' already exists.");

        var createResult = Language.Create(
            SequentialGuid.NewSequentialGuidAtEnd(),
            request.Code,
            request.Name,
            request.NativeName,
            request.IsDefault);

        if (createResult.IsFailure) return createResult.Error;

        await repository.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateLanguageCachesAsync(cancellationToken);

        return GetAllLanguagesQueryHandler.MapToDto(createResult.Value);
    }

    private async Task InvalidateLanguageCachesAsync(CancellationToken ct)
    {
        await cache.RemoveAsync(I18nCacheKeys.LanguagesAll, ct);
        await cache.RemoveAsync(I18nCacheKeys.LanguagesActive, ct);
    }
}
