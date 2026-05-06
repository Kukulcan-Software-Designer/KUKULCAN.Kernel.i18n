namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="Code"></param>
/// <param name="Name"></param>
/// <param name="NativeName"></param>
/// <param name="IsDefault"></param>
/// <param name="IsActive"></param>
/// <param name="CreatedAt"></param>
/// <param name="CreatedBy"></param>
/// <param name="UpdatedAt"></param>
/// <param name="UpdatedBy"></param>
public record LanguageDto(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
