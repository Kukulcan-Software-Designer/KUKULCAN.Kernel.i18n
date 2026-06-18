using KUKULCAN.Kernel.i18n.Domain.DTOs;

namespace KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.UpdateLanguage;

/// <summary>
/// Representa una solicitud para actualizar los datos de un idioma existente, incluyendo su código, nombre y nombre
/// nativo.
/// </summary>
/// <param name="Code">El código único del idioma que se va a actualizar. No puede ser nulo ni estar vacío.</param>
/// <param name="Name">El nuevo nombre descriptivo del idioma. No puede ser nulo ni estar vacío.</param>
/// <param name="NativeName">El nuevo nombre nativo del idioma. No puede ser nulo ni estar vacío.</param>
public record UpdateLanguageCommand(string Code, string Name, string NativeName) : IRequest<Result<LanguageDto>>;
