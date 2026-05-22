using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

/// <summary>
/// Represents the GetTranslationsPagedQueryHandler type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
public sealed class GetTranslationsPagedQueryHandler(ITranslationRepository repository) : IRequestHandler<GetTranslationsPagedQuery, Result<PagedResult<TranslationDto>>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<PagedResult<TranslationDto>>> Handle(GetTranslationsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedAsync(request.Pagination.Page, request.Pagination.PageSize, request.ModuleFilter?.ToUpperInvariant(),
            request.LanguageFilter?.ToLowerInvariant(), cancellationToken);
        var dtos = items.Select(MapToDto).ToList();

        // Use SharedKernel's PagedResult.Create
        return PagedResult<TranslationDto>.Create(dtos, total, request.Pagination);
    }

    // ── GET VARIANTS ──────────────────────────────────────────────────────────

    internal static TranslationDto MapToDto(Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed,
            t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy);
}
