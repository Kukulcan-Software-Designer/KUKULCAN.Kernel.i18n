namespace KUKULCAN.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// Represents the UpdateTranslationRequest record.
/// </summary>
/// <param name="Text">The Text parameter.</param>
/// <param name="Context">The Context parameter.</param>
public record UpdateTranslationRequest(string Text, string? Context = null);
