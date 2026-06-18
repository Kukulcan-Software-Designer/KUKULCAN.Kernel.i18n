using KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;
using KUKULCAN.Kernel.i18n.Application.Common;
using KUKULCAN.Kernel.Primitives.Interfaces;

namespace KUKULCAN.Kernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;

/// <summary>
/// Handles the deletion of a currency format for a specified language and currency code.
/// </summary>
/// <remarks>Removes the currency format from both the data store and the cache to ensure consistency. This
/// handler is typically used in command processing scenarios to enforce business rules and maintain data
/// integrity.</remarks>
/// <param name="repository">The repository used to access and manage currency format entities.</param>
/// <param name="unitOfWork">The unit of work used to persist changes to the data store.</param>
/// <param name="cache">The cache service used to remove cached currency format data after deletion.</param>
public sealed class DeleteCurrencyFormatCommandHandler(ICurrencyFormatRepository repository, IUnitOfWork unitOfWork, ICacheService cache) :
    IRequestHandler<DeleteCurrencyFormatCommand, Result>
{
    /// <summary>
    /// Handles the deletion of a currency format for a specified language and currency code.
    /// </summary>
    /// <param name="request">The command containing the details of the currency format to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the deletion operation.</returns>
    public async Task<Result> Handle(DeleteCurrencyFormatCommand request, CancellationToken cancellationToken)
    {
        var langResult = LanguageCode.Create(request.LanguageCode);
        if (langResult.IsFailure)
            return langResult.Error;

        var lang = langResult.Value;
        var currency = request.CurrencyCode.ToUpperInvariant();

        var format = await repository.FindAsync(lang, currency, cancellationToken);
        if (format is null)
            return Error.NotFound("CurrencyFormat.NotFound", $"No currency format for '{currency}' in language '{lang.Value}'.");

        repository.Remove(format);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(I18NCacheKeys.CurrencyFormat(lang.Value, currency), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.CurrencyFormats(lang.Value), cancellationToken);

        return Result.Ok();
    }
}
