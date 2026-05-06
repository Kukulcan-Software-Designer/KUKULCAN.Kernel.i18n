using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.Infrastructure.Primitives;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="translationRepo"></param>
/// <param name="languageRepo"></param>
/// <param name="unitOfWork"></param>
/// <param name="cache"></param>
public sealed class CreateTranslationCommandHandler(ITranslationRepository translationRepo, ILanguageRepository languageRepo, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<CreateTranslationCommand, Result<TranslationDto>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<TranslationDto>> Handle(CreateTranslationCommand request, CancellationToken cancellationToken)
    {
        var codeResult = TranslationCode.From(request.Code);
        if (codeResult.IsFailure) return codeResult.Error;

        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure) return langResult.Error;

        var code = codeResult.Value;
        var lang = langResult.Value;

        // Verify language exists and is active
        var language = await languageRepo.GetByCodeAsync(lang.Value, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{lang.Value}' was not found.");

        if (!language.IsActive)
            return Error.Conflict(
                "Language.Inactive",
                $"Language '{lang.Value}' is inactive. Translations cannot be added to inactive languages.");

        // Enforce uniqueness (code + language)
        if (await translationRepo.ExistsAsync(code, lang, cancellationToken))
            return Error.Conflict(
                "Translation.Duplicate",
                $"A translation for '{code.Value}' in language '{lang.Value}' already exists.");

        // Create entity using Result pattern — use SequentialGuid for PostgreSQL optimisation
        var createResult = Translation.Create(
            SequentialGuid.NewSequentialGuidAtEnd(),
            request.Code,
            request.LanguageCode,
            request.Text,
            request.Context,
            request.MaxLength);

        if (createResult.IsFailure) return createResult.Error;

        await translationRepo.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await cache.RemoveAsync(I18nCacheKeys.Translation(code.Value, lang.Value), cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.ModuleTranslations(code.Module, lang.Value), cancellationToken);

        return MapToDto(createResult.Value);
    }

    internal static TranslationDto MapToDto(Translation t) =>
        new(t.Id, t.Code.Value, t.Code.Module, t.LanguageCode.Value,
            t.Text, t.Context, t.MaxLength, t.IsReviewed,
            t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy);
}

