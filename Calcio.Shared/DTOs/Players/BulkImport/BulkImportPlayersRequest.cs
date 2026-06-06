namespace Calcio.Shared.DTOs.Players.BulkImport;

/// <summary>
/// Request to import a batch of validated player rows.
/// </summary>
/// <param name="Rows">The validated rows to import. Only rows with IsMarkedForImport=true and IsValid=true will be imported.</param>
public sealed record BulkImportPlayersRequest(
    List<PlayerImportRowDto> Rows);
