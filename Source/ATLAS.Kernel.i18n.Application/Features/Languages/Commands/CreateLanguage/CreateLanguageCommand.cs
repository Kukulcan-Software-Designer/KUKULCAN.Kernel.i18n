using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;

/// <summary>
/// Represents a command to create a new language with the specified code, display name, and native name.
/// </summary>
/// <remarks>Use this command to add a new language to the system. If IsDefault is set to true, the new language
/// will become the default language for the application.</remarks>
/// <param name="Code">The ISO code that uniquely identifies the language. Cannot be null or empty.</param>
/// <param name="Name">The display name of the language in the application's primary language. Cannot be null or empty.</param>
/// <param name="NativeName">The name of the language as written in its own script. Cannot be null or empty.</param>
/// <param name="IsDefault">true to set the new language as the default; otherwise, false.</param>
public record CreateLanguageCommand(string Code, string Name, string NativeName, bool IsDefault = false) : IRequest<Result<LanguageDto>>;
