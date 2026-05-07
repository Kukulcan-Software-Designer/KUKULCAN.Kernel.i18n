namespace ATLAS.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// Represents the UpdateLanguageRequest record.
/// </summary>
/// <param name="Name">The Name parameter.</param>
/// <param name="NativeName">The NativeName parameter.</param>
public record UpdateLanguageRequest(string Name, string NativeName);
