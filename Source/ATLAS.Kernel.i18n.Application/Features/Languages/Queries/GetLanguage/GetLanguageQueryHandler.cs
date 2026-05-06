using ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;
using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;

/// <summary>
/// Handles queries to retrieve language information by code.
/// </summary>
/// <remarks>This handler processes GetLanguageQuery requests and returns the corresponding language data as a
/// LanguageDto wrapped in a Result. If the specified language code does not exist in the repository, a not found error
/// is returned. This class is typically used within a CQRS pattern to separate query logic from command
/// logic.</remarks>
/// <remarks>
/// Initializes a new instance of the GetLanguageQueryHandler class with the specified language repository.
/// </remarks>
/// <param name="repository">The repository used to access language data. Cannot be null.</param>
public sealed class GetLanguageQueryHandler(ILanguageRepository repository) : IRequestHandler<GetLanguageQuery, Result<LanguageDto>>
{
    /// <summary>
    /// Handles a request to retrieve a language by its code.
    /// </summary>
    /// <param name="request">The query containing the code of the language to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result object with the language
    /// data as a LanguageDto if found; otherwise, an error indicating that the language was not found.</returns>
    public async Task<Result<LanguageDto>> Handle(GetLanguageQuery request, CancellationToken cancellationToken)
    {
        var language = await repository.GetByCodeAsync(request.Code, cancellationToken);

        return language is null
            ? Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.")
            : GetAllLanguagesQueryHandler.MapToDto(language);
    }
}
