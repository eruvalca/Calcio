using System.Globalization;
using System.Text;

using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Players;
using Calcio.Shared.Entities;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions.Players;
using Calcio.Shared.Results;

using ClosedXML.Excel;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Services.Players;

public partial class PlayersService
{
    private const int MaxPlayersPerImport = 1000;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public async Task<ServiceResult<PlayerImportResultDto>> BulkImportPlayersAsync(
        long clubId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Validate file size by checking stream length if possible
        if (fileStream.CanSeek && fileStream.Length > MaxFileSizeBytes)
        {
            return ServiceProblem.BadRequest($"File size exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB.");
        }

        await using var dbContext = await readWriteDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Create import record
        var import = new PlayerImportEntity
        {
            FileName = fileName,
            ClubId = clubId,
            Status = PlayerImportStatus.Processing,
            CreatedById = CurrentUserId
        };

        await dbContext.PlayerImports.AddAsync(import, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Parse file based on content type
            var rows = contentType.ToLowerInvariant() switch
            {
                "text/csv" or "application/vnd.ms-excel" => await ParseCsvAsync(fileStream, cancellationToken),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ParseExcel(fileStream),
                _ => throw new InvalidOperationException($"Unsupported file type: {contentType}")
            };

            if (rows.Count == 0)
            {
                import.Status = PlayerImportStatus.Failed;
                import.ErrorMessage = "No data rows found in the file.";
                import.CompletedAt = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync(cancellationToken);

                LogImportFailed(logger, import.ImportId, clubId, "No data rows", CurrentUserId);
                return ServiceProblem.BadRequest("No data rows found in the file.");
            }

            if (rows.Count > MaxPlayersPerImport)
            {
                import.Status = PlayerImportStatus.Failed;
                import.ErrorMessage = $"File contains {rows.Count} rows, exceeding the maximum of {MaxPlayersPerImport}.";
                import.CompletedAt = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync(cancellationToken);

                LogImportFailed(logger, import.ImportId, clubId, "Too many rows", CurrentUserId);
                return ServiceProblem.BadRequest($"File contains {rows.Count} rows, exceeding the maximum of {MaxPlayersPerImport}.");
            }

            // Validate and import rows
            import.TotalRows = rows.Count;
            var importResults = await ValidateAndImportRowsAsync(dbContext, clubId, import.ImportId, rows, cancellationToken);

            import.SuccessfulRows = importResults.Count(r => r.IsSuccess);
            import.FailedRows = importResults.Count(r => !r.IsSuccess);

            // Check if there are any validation errors
            if (import.FailedRows > 0)
            {
                import.Status = PlayerImportStatus.Completed;
                import.ErrorMessage = $"{import.FailedRows} row(s) failed validation and were not imported.";
                import.CompletedAt = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync(cancellationToken);

                LogImportCompleted(logger, import.ImportId, clubId, import.SuccessfulRows, import.FailedRows, CurrentUserId);
                return ServiceProblem.BadRequest($"{import.FailedRows} row(s) failed validation. No players were imported. Please review the import status for details.");
            }

            // All rows valid, mark as completed
            import.Status = PlayerImportStatus.Completed;
            import.CompletedAt = DateTimeOffset.Now;
            await dbContext.SaveChangesAsync(cancellationToken);

            LogImportCompleted(logger, import.ImportId, clubId, import.SuccessfulRows, import.FailedRows, CurrentUserId);

            return import.ToPlayerImportResultDto();
        }
        catch (Exception ex)
        {
            import.Status = PlayerImportStatus.Failed;
            import.ErrorMessage = ex.Message;
            import.CompletedAt = DateTimeOffset.Now;
            await dbContext.SaveChangesAsync(cancellationToken);

            LogImportException(logger, ex, import.ImportId, clubId, CurrentUserId);
            return ServiceProblem.ServerError($"An error occurred during import: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PlayerImportStatusDto>> GetImportStatusAsync(
        long clubId,
        long importId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        var import = await dbContext.PlayerImports
            .Include(i => i.Rows)
                .ThenInclude(r => r.CreatedPlayer)
            .FirstOrDefaultAsync(i => i.ImportId == importId && i.ClubId == clubId, cancellationToken);

        if (import is null)
        {
            return ServiceProblem.NotFound("Import not found.");
        }

        return import.ToPlayerImportStatusDto();
    }

    public Stream GenerateImportTemplate()
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write headers
        csv.WriteField("FirstName");
        csv.WriteField("LastName");
        csv.WriteField("DateOfBirth");
        csv.WriteField("GraduationYear");
        csv.WriteField("Gender");
        csv.WriteField("JerseyNumber");
        csv.WriteField("TryoutNumber");
        csv.NextRecord();

        // Write sample row
        csv.WriteField("John");
        csv.WriteField("Doe");
        csv.WriteField("2010-01-15");
        csv.WriteField("2028");
        csv.WriteField("Male");
        csv.WriteField("10");
        csv.WriteField("123");
        csv.NextRecord();

        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private async Task<List<PlayerImportRowData>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        var rows = new List<PlayerImportRowData>();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant().Replace(" ", string.Empty)
        };

        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            rows.Add(new PlayerImportRowData
            {
                RowNumber = rowNumber,
                FirstName = csv.GetField("FirstName")?.Trim(),
                LastName = csv.GetField("LastName")?.Trim(),
                DateOfBirth = csv.GetField("DateOfBirth")?.Trim(),
                GraduationYear = csv.GetField("GraduationYear")?.Trim(),
                Gender = csv.GetField("Gender")?.Trim(),
                JerseyNumber = csv.GetField("JerseyNumber")?.Trim(),
                TryoutNumber = csv.GetField("TryoutNumber")?.Trim(),
                RawData = string.Join("|", csv.Parser.Record ?? [])
            });
        }

        return rows;
    }

    private List<PlayerImportRowData> ParseExcel(Stream stream)
    {
        var rows = new List<PlayerImportRowData>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        // Find header row
        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        for (int col = 1; col <= headerRow.CellsUsed().Count(); col++)
        {
            var header = headerRow.Cell(col).GetString().Trim().Replace(" ", string.Empty);
            headers[header] = col;
        }

        // Read data rows
        var dataRows = worksheet.RowsUsed().Skip(1);
        var rowNumber = 1;

        foreach (var row in dataRows)
        {
            rowNumber++;
            var rowData = new PlayerImportRowData
            {
                RowNumber = rowNumber,
                FirstName = GetCellValue(row, headers, "FirstName"),
                LastName = GetCellValue(row, headers, "LastName"),
                DateOfBirth = GetCellValue(row, headers, "DateOfBirth"),
                GraduationYear = GetCellValue(row, headers, "GraduationYear"),
                Gender = GetCellValue(row, headers, "Gender"),
                JerseyNumber = GetCellValue(row, headers, "JerseyNumber"),
                TryoutNumber = GetCellValue(row, headers, "TryoutNumber")
            };

            var rawDataValues = new List<string>();
            for (int col = 1; col <= row.CellsUsed().Count(); col++)
            {
                rawDataValues.Add(row.Cell(col).GetString());
            }
            rowData.RawData = string.Join("|", rawDataValues);

            rows.Add(rowData);
        }

        return rows;
    }

    private static string? GetCellValue(IXLRow row, Dictionary<string, int> headers, string headerName)
    {
        if (headers.TryGetValue(headerName, out var colIndex))
        {
            return row.Cell(colIndex).GetString().Trim();
        }
        return null;
    }

    private async Task<List<PlayerImportRowEntity>> ValidateAndImportRowsAsync(
        ReadWriteDbContext dbContext,
        long clubId,
        long importId,
        List<PlayerImportRowData> rows,
        CancellationToken cancellationToken)
    {
        var results = new List<PlayerImportRowEntity>();
        var playersToCreate = new List<PlayerEntity>();

        // First pass: validate all rows
        foreach (var row in rows)
        {
            var (isValid, errorMessage, player) = ValidateAndCreatePlayer(row, clubId);

            var rowEntity = new PlayerImportRowEntity
            {
                ImportId = importId,
                RowNumber = row.RowNumber,
                IsSuccess = isValid,
                ErrorMessage = errorMessage,
                RawData = row.RawData.Length > 4000 ? row.RawData[..4000] : row.RawData,
                CreatedById = CurrentUserId
            };

            results.Add(rowEntity);

            if (isValid && player is not null)
            {
                playersToCreate.Add(player);
            }
        }

        // Check if any validation failed
        if (results.Any(r => !r.IsSuccess))
        {
            // Don't import any players if validation failed
            // Just save the row entities with error messages
            await dbContext.PlayerImportRows.AddRangeAsync(results, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return results;
        }

        // Second pass: all validations passed, import all players
        await dbContext.Players.AddRangeAsync(playersToCreate, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Update row entities with created player IDs
        for (int i = 0; i < results.Count; i++)
        {
            results[i].CreatedPlayerId = playersToCreate[i].PlayerId;
        }

        await dbContext.PlayerImportRows.AddRangeAsync(results, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return results;
    }

    private (bool IsValid, string? ErrorMessage, PlayerEntity? Player) ValidateAndCreatePlayer(
        PlayerImportRowData row,
        long clubId)
    {
        var errors = new List<string>();

        // Validate FirstName
        if (string.IsNullOrWhiteSpace(row.FirstName))
        {
            errors.Add("FirstName is required");
        }
        else if (row.FirstName.Length > 100)
        {
            errors.Add("FirstName must be 100 characters or less");
        }

        // Validate LastName
        if (string.IsNullOrWhiteSpace(row.LastName))
        {
            errors.Add("LastName is required");
        }
        else if (row.LastName.Length > 100)
        {
            errors.Add("LastName must be 100 characters or less");
        }

        // Validate DateOfBirth
        DateOnly dateOfBirth = default;
        if (string.IsNullOrWhiteSpace(row.DateOfBirth))
        {
            errors.Add("DateOfBirth is required");
        }
        else if (!DateOnly.TryParse(row.DateOfBirth, CultureInfo.InvariantCulture, out dateOfBirth))
        {
            errors.Add("DateOfBirth must be a valid date (yyyy-MM-dd format)");
        }

        // Validate GraduationYear
        int graduationYear = 0;
        if (string.IsNullOrWhiteSpace(row.GraduationYear))
        {
            errors.Add("GraduationYear is required");
        }
        else if (!int.TryParse(row.GraduationYear, out graduationYear))
        {
            errors.Add("GraduationYear must be a valid integer");
        }
        else if (graduationYear < 2000 || graduationYear > 2100)
        {
            errors.Add("GraduationYear must be between 2000 and 2100");
        }

        // Validate Gender (optional)
        Gender? gender = null;
        if (!string.IsNullOrWhiteSpace(row.Gender))
        {
            if (!Enum.TryParse<Gender>(row.Gender, true, out var parsedGender))
            {
                errors.Add("Gender must be Male, Female, or Other");
            }
            else
            {
                gender = parsedGender;
            }
        }

        // Validate JerseyNumber (optional)
        int? jerseyNumber = null;
        if (!string.IsNullOrWhiteSpace(row.JerseyNumber))
        {
            if (!int.TryParse(row.JerseyNumber, out var parsedJerseyNumber))
            {
                errors.Add("JerseyNumber must be a valid integer");
            }
            else if (parsedJerseyNumber < 0 || parsedJerseyNumber > 999)
            {
                errors.Add("JerseyNumber must be between 0 and 999");
            }
            else
            {
                jerseyNumber = parsedJerseyNumber;
            }
        }

        // Validate TryoutNumber (optional)
        int? tryoutNumber = null;
        if (!string.IsNullOrWhiteSpace(row.TryoutNumber))
        {
            if (!int.TryParse(row.TryoutNumber, out var parsedTryoutNumber))
            {
                errors.Add("TryoutNumber must be a valid integer");
            }
            else if (parsedTryoutNumber < 0 || parsedTryoutNumber > 9999)
            {
                errors.Add("TryoutNumber must be between 0 and 9999");
            }
            else
            {
                tryoutNumber = parsedTryoutNumber;
            }
        }

        if (errors.Count > 0)
        {
            return (false, string.Join("; ", errors), null);
        }

        var player = new PlayerEntity
        {
            FirstName = row.FirstName!,
            LastName = row.LastName!,
            DateOfBirth = dateOfBirth,
            GraduationYear = graduationYear,
            Gender = gender,
            JerseyNumber = jerseyNumber,
            TryoutNumber = tryoutNumber,
            ClubId = clubId,
            CreatedById = CurrentUserId
        };

        return (true, null, player);
    }

    private class PlayerImportRowData
    {
        public int RowNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? GraduationYear { get; set; }
        public string? Gender { get; set; }
        public string? JerseyNumber { get; set; }
        public string? TryoutNumber { get; set; }
        public string RawData { get; set; } = string.Empty;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Import {ImportId} completed for club {ClubId} by user {UserId}. Success: {SuccessCount}, Failed: {FailedCount}")]
    private static partial void LogImportCompleted(ILogger logger, long importId, long clubId, int successCount, int failedCount, long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Import {ImportId} failed for club {ClubId} by user {UserId}. Reason: {Reason}")]
    private static partial void LogImportFailed(ILogger logger, long importId, long clubId, string reason, long userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception during import {ImportId} for club {ClubId} by user {UserId}")]
    private static partial void LogImportException(ILogger logger, Exception ex, long importId, long clubId, long userId);
}
