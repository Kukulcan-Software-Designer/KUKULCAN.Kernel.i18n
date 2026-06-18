using KUKULCAN.Kernel.i18n.Domain.DTOs;

namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;

/// <summary>
/// Represents the BulkUpsertTranslationsCommand record.
/// </summary>
/// <param name="Items">The Items parameter.</param>
public record BulkUpsertTranslationsCommand(IReadOnlyList<BulkTranslationDto> Items) : IRequest<Result<BulkUpsertResultDto>>;
