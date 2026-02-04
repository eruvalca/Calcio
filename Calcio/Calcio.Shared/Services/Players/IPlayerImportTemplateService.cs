namespace Calcio.Shared.Services.Players;

/// <summary>
/// Service for generating player import templates.
/// </summary>
public interface IPlayerImportTemplateService
{
    /// <summary>
    /// Generates a CSV template for player import.
    /// </summary>
    /// <returns>The CSV template content as bytes.</returns>
    byte[] GenerateCsvTemplate();

    /// <summary>
    /// Generates an Excel (.xlsx) template for player import.
    /// </summary>
    /// <returns>The Excel template content as bytes.</returns>
    byte[] GenerateExcelTemplate();
}
