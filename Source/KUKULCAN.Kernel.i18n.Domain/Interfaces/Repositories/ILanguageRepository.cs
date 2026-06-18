namespace KUKULCAN.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="Language"/>.
/// Extends <see cref="IRepository{T,TId}"/> from <c>Atlas.SharedKernel.Abstractions</c>,
/// which provides <c>GetByIdAsync</c>, <c>ListAllAsync</c>, <c>AddAsync</c>,
/// <c>Update</c>, and <c>ExistsAsync</c>.
/// </summary>
public interface ILanguageRepository : IRepository<Language, Guid>
{
    /// <summary>
    /// Returns the language with the given BCP-47 code, or <c>null</c>.
    /// </summary>
    /// <param name="bcp47Code">The BCP-47 language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The language if found; otherwise, <c>null</c>.</returns>
    Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default);

    /// <summary>
    /// Returns all active languages ordered by display name.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of active languages.</returns>
    Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the language currently marked as the platform default, or <c>null</c> if none is set.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The default language if set; otherwise, <c>null</c>.</returns>
    Task<Language?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks whether a language with the given BCP-47 code already exists.
    /// </summary>
    /// <param name="bcp47Code">The BCP-47 language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the language exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default);
}
