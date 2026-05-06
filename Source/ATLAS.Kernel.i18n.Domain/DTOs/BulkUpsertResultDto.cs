namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="Created"></param>
/// <param name="Updated"></param>
/// <param name="Errors"></param>
public record BulkUpsertResultDto(int Created, int Updated, IReadOnlyList<string> Errors);
