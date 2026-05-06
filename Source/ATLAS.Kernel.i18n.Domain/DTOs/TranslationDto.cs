namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="Code"></param>
/// <param name="Module"></param>
/// <param name="LanguageCode"></param>
/// <param name="Text"></param>
/// <param name="Context"></param>
/// <param name="MaxLength"></param>
/// <param name="IsReviewed"></param>
/// <param name="CreatedAt"></param>
/// <param name="CreatedBy"></param>
/// <param name="UpdatedAt"></param>
/// <param name="UpdatedBy"></param>
public record TranslationDto(
    Guid Id,
    string Code,
    string Module,
    string LanguageCode,
    string Text,
    string? Context,
    int? MaxLength,
    bool IsReviewed,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
