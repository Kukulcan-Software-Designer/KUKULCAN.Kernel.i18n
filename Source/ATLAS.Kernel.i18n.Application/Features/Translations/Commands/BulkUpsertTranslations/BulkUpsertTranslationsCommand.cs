using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;

/// <summary>
/// 
/// </summary>
/// <param name="Items"></param>
public record BulkUpsertTranslationsCommand(IReadOnlyList<BulkTranslationDto> Items) : IRequest<Result<BulkUpsertResultDto>>;
