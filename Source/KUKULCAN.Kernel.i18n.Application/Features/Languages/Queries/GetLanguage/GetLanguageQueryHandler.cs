using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;

namespace KUKULCAN.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;

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
public sealed class GetLanguageQueryHandler(ILanguageRepository repository)
        : IRequestHandler<GetLanguageQuery, Result<LanguageDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<LanguageDto>> Handle(GetLanguageQuery request, CancellationToken cancellationToken)
    {
        var language = await repository.GetByCodeAsync(request.Code, cancellationToken);

        return language is null
            ? Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.")
            : GetAllLanguagesQueryHandler.MapToDto(language);
    }
}
