using ATLAS.Kernel.i18n.Domain.DTOs;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.Infrastructure.Primitives;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;

/// <summary>
/// Represents the BulkUpsertTranslationsCommandHandler type.
/// </summary>
/// <param name="translationRepo">The translationRepo parameter.</param>
/// <param name="languageRepo">The languageRepo parameter.</param>
/// <param name="unitOfWork">The unitOfWork parameter.</param>
/// <param name="cache">The cache parameter.</param>
public sealed class BulkUpsertTranslationsCommandHandler(ITranslationRepository translationRepo, ILanguageRepository languageRepo, IUnitOfWork unitOfWork,
    ICacheService cache): IRequestHandler<BulkUpsertTranslationsCommand, Result<BulkUpsertResultDto>>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<BulkUpsertResultDto>> Handle(BulkUpsertTranslationsCommand request, CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var errors = new List<string>();
        var moduleLangPairs = new HashSet<(string, string)>();

        // Pre-load active language codes
        var activeLangs = (await languageRepo.GetAllActiveAsync(cancellationToken))
            .Select(l => l.Code.ToLowerInvariant())
            .ToHashSet();

        foreach (var item in request.Items)
        {
            var codeResult = TranslationCode.From(item.Code);
            if (codeResult.IsFailure)
            {
                errors.Add($"{item.Code}: {codeResult.Error.Message}");
                continue;
            }

            var langResult = LanguageCode.Create(item.LanguageCode);

            if (langResult.IsFailure)
            {
                errors.Add($"{item.Code}/{item.LanguageCode}: {langResult.Error.Message}");
                continue;
            }

            var code = codeResult.Value;
            var lang = langResult.Value;

            if (!activeLangs.Contains(lang.Value.ToLowerInvariant()))
            {
                errors.Add($"{item.Code}/{lang.Value}: Language not found or inactive.");
                continue;
            }

            var existing = await translationRepo.FindAsync(code, lang, cancellationToken);

            if (existing is null)
            {
                var createResult = Translation.Create(
                    SequentialGuid.NewSequentialGuidAtEnd(),
                    item.Code, item.LanguageCode, item.Text, item.Context, item.MaxLength);

                if (createResult.IsFailure)
                {
                    errors.Add($"{item.Code}: {createResult.Error.Message}");
                    continue;
                }
                await translationRepo.AddAsync(createResult.Value, cancellationToken);
                created++;
            }
            else
            {
                var updateResult = existing.UpdateText(item.Text);

                if (updateResult.IsFailure)
                {
                    errors.Add($"{item.Code}: {updateResult.Error.Message}");
                    continue;
                }
                existing.UpdateContext(item.Context);
                translationRepo.Update(existing);
                updated++;
            }
            moduleLangPairs.Add((code.Module, lang.Value));
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        // Invalidate module caches
        foreach (var (module, lang) in moduleLangPairs)
            await cache.RemoveAsync(I18NCacheKeys.ModuleTranslations(module, lang), cancellationToken);

        return new BulkUpsertResultDto(created, updated, errors);
    }
}
