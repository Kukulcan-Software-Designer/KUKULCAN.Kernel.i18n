using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

/// <summary>
/// 
/// </summary>
/// <param name="repository"></param>
public sealed class GetTranslationsPagedQueryHandler(ITranslationRepository repository) : IRequestHandler<GetTranslationsPagedQuery, Result<PagedResult<TranslationDto>>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<PagedResult<TranslationDto>>> Handle(GetTranslationsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedAsync(
            request.Pagination.Page,
            request.Pagination.PageSize,
            request.ModuleFilter?.ToUpperInvariant(),
            request.LanguageFilter?.ToLowerInvariant(),
            cancellationToken);

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
