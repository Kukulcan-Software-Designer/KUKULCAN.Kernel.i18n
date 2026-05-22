namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Provides functionality for this member.
/// </summary>
/// <param name="Id">The Id parameter.</param>
/// <param name="Code">The Code parameter.</param>
/// <param name="Module">The Module parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="Text">The Text parameter.</param>
/// <param name="Context">The Context parameter.</param>
/// <param name="MaxLength">The MaxLength parameter.</param>
/// <param name="IsReviewed">The IsReviewed parameter.</param>
/// <param name="CreatedAt">The CreatedAt parameter.</param>
/// <param name="CreatedBy">The CreatedBy parameter.</param>
/// <param name="UpdatedAt">The UpdatedAt parameter.</param>
/// <param name="UpdatedBy">The UpdatedBy parameter.</param>
public record TranslationDto(Guid Id, string Code, string Module, string LanguageCode, string Text, string? Context, int? MaxLength,
    bool IsReviewed, DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset? UpdatedAt, string? UpdatedBy);
