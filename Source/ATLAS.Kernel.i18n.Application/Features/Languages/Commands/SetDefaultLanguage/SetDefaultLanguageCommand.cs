namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;

/// <summary>
/// Represents a request to set the application's default language using the specified language code.
/// </summary>
/// <param name="Code">The language code to set as the default. Must be a valid ISO language identifier and cannot be null or empty.</param>
public record SetDefaultLanguageCommand(string Code) : IRequest<Result>;
