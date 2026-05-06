using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.BulkUpsertTranslations;

/// <summary>
/// 
/// </summary>
public sealed class BulkUpsertTranslationsCommandValidator : AbstractValidator<BulkUpsertTranslationsCommand>
{
    /// <summary>
    /// 
    /// </summary>
    public BulkUpsertTranslationsCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.")
            .Must(i => i.Count <= 5000).WithMessage("Maximum 5,000 items per bulk operation.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Code).NotEmpty();
            item.RuleFor(i => i.LanguageCode).NotEmpty();
            item.RuleFor(i => i.Text).NotEmpty().MaximumLength(4000);
        });
    }
}
