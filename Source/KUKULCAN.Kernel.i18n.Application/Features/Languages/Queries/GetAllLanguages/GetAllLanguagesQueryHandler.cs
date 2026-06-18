using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;

namespace KUKULCAN.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;

/// <summary>
/// Handles queries to retrieve all available languages, optionally filtering to only active languages.
/// </summary>
/// <remarks>This handler supports retrieving either all languages or only those marked as active, depending on
/// the query parameters. The result contains a read-only list of language data transfer objects suitable for client
/// consumption.</remarks>
/// <param name="repository">The repository used to access language data.</param>
public sealed class GetAllLanguagesQueryHandler(ILanguageRepository repository)
        : IRequestHandler<GetAllLanguagesQuery, Result<IReadOnlyList<LanguageDto>>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(GetAllLanguagesQuery request, CancellationToken cancellationToken)
    {
        var languages = request.ActiveOnly
            ? await repository.GetAllActiveAsync(cancellationToken)
            : await repository.ListAllAsync(cancellationToken);

        return Result<IReadOnlyList<LanguageDto>>.Ok(
            languages.Select(MapToDto).ToList());
    }

    internal static LanguageDto MapToDto(Language l) =>
        new(l.Id, l.Code, l.Name, l.NativeName, l.IsDefault, l.IsActive,
            l.CreatedAt, l.CreatedBy, l.UpdatedAt, l.UpdatedBy);
}
