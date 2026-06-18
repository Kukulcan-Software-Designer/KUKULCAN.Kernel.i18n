namespace KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;

/// <summary>
/// Represents a request to set the active status of a language identified by its code.
/// </summary>
/// <param name="Code">The code that uniquely identifies the language to update. Cannot be null or empty.</param>
/// <param name="IsActive">A value indicating whether the language should be set as active. Set to <see langword="true"/> to activate the
/// language; otherwise, <see langword="false"/>.</param>
public record SetLanguageActiveCommand(string Code, bool IsActive) : IRequest<Result>;
