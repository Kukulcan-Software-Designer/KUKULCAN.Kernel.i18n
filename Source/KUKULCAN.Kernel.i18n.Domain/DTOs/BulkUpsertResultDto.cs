namespace KUKULCAN.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Represents the BulkUpsertResultDto record.
/// </summary>
/// <param name="Created">The Created parameter.</param>
/// <param name="Updated">The Updated parameter.</param>
/// <param name="Errors">The Errors parameter.</param>
public record BulkUpsertResultDto(int Created, int Updated, IReadOnlyList<string> Errors);
