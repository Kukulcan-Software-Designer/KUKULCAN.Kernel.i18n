namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Provides functionality for this member.
/// </summary>
/// <param name="Id">The Id parameter.</param>
/// <param name="Code">The Code parameter.</param>
/// <param name="Name">The Name parameter.</param>
/// <param name="NativeName">The NativeName parameter.</param>
/// <param name="IsDefault">The IsDefault parameter.</param>
/// <param name="IsActive">The IsActive parameter.</param>
/// <param name="CreatedAt">The CreatedAt parameter.</param>
/// <param name="CreatedBy">The CreatedBy parameter.</param>
/// <param name="UpdatedAt">The UpdatedAt parameter.</param>
/// <param name="UpdatedBy">The UpdatedBy parameter.</param>
public record LanguageDto(Guid Id, string Code, string Name, string NativeName, bool IsDefault,
    bool IsActive, DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset? UpdatedAt, string? UpdatedBy);
