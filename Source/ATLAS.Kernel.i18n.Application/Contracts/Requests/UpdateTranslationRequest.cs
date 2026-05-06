namespace ATLAS.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// 
/// </summary>
/// <param name="Text"></param>
/// <param name="Context"></param>
public record UpdateTranslationRequest(string Text, string? Context = null);
