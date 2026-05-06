namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="Language"/>.
/// Extends <see cref="IRepository{T,TId}"/> from <c>Atlas.SharedKernel.Abstractions</c>,
/// which provides <c>GetByIdAsync</c>, <c>ListAllAsync</c>, <c>AddAsync</c>,
/// <c>Update</c>, and <c>ExistsAsync</c>.
/// </summary>
public interface ILanguageRepository : IRepository<Language, Guid>
{
    /// <summary>Returns the language with the given BCP-47 code, or <c>null</c>.</summary>
    Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default);

    /// <summary>Returns all active languages ordered by display name.</summary>
    Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Returns the language currently marked as the platform default.</summary>
    Task<Language?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>Checks whether a language with the given BCP-47 code already exists.</summary>
    Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default);
}
