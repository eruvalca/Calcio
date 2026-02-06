using System.Data;
using System.Globalization;
using System.Text;

using Calcio.Data.Contexts;
using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Enums;
using Calcio.Shared.Extensions;
using Calcio.Shared.Results;
using Calcio.Shared.Services.Players;
using Calcio.Shared.Validation;

using Microsoft.VisualBasic.FileIO;

using Microsoft.EntityFrameworkCore;

namespace Calcio.Services.Players;

/// <summary>
/// Service for parsing and validating player import files using CSV parsing.
/// </summary>
public partial class PlayerImportParserService(
    IDbContextFactory<ReadOnlyDbContext> readOnlyDbContextFactory,
    ILogger<PlayerImportParserService> logger) : IPlayerImportParserService
{
    private static readonly string[] SupportedExtensions = [".csv"];
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy",
        "yyyy/MM/dd", "MM-dd-yyyy", "dd-MM-yyyy"
    ];

    public async Task<ServiceResult<BulkValidateResultDto>> ParseAndValidateAsync(
        Stream fileStream,
        string fileName,
        long clubId,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!SupportedExtensions.Contains(extension))
        {
            return ServiceProblem.BadRequest("Unsupported file format. Please upload a CSV file (.csv). Save Excel or Google Sheets files as CSV before uploading.");
        }

        try
        {
            // Ensure a seekable stream for CSV parsing
            Stream workingStream = fileStream;
            MemoryStream? memoryStream = null;

            if (!fileStream.CanSeek)
            {
                memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                workingStream = memoryStream;
            }

            try
            {
                var table = ReadCsvDataTable(workingStream, cancellationToken);

                if (table.Rows.Count == 0)
                {
                    return ServiceProblem.BadRequest("The file is empty or contains no data rows.");
                }

                // Map columns
                var columnMappings = MapColumns(table.Columns);
                var missingRequired = columnMappings
                    .Where(m => m.IsRequired && !m.IsDetected)
                    .Select(m => m.FieldName)
                    .ToList();

                if (missingRequired.Count > 0)
                {
                    LogMissingRequiredColumns(logger, string.Join(", ", missingRequired), fileName);

                    // Still return result with column mappings so UI can show what's missing
                    return new BulkValidateResultDto(
                        Rows: [],
                        ColumnMappings: columnMappings,
                        MissingRequiredColumns: missingRequired,
                        ValidCount: 0,
                        ErrorCount: 0,
                        WarningCount: 0,
                        DuplicateInFileCount: 0,
                        DuplicateInDatabaseCount: 0);
                }

                // Create column index lookup
                var columnIndexMap = CreateColumnIndexMap(table.Columns, columnMappings);

                // Parse rows
                var rows = ParseRows(table, columnIndexMap);

                // Validate rows and check for duplicates
                var validatedRows = await ValidateRowsAsync(rows, clubId, cancellationToken);

                var result = CreateResult(validatedRows, columnMappings);
                LogParseComplete(logger, fileName, result.ValidCount, result.ErrorCount, result.WarningCount);

                return result;
            }
            finally
            {
                memoryStream?.Dispose();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogParseError(logger, fileName, ex.Message);
            return ServiceProblem.BadRequest($"Failed to parse the file: {ex.Message}");
        }
    }

    public async Task<ServiceResult<BulkValidateResultDto>> RevalidateRowsAsync(
        List<PlayerImportRowDto> rows,
        long clubId,
        CancellationToken cancellationToken)
    {
        // Clear previous validation state
        foreach (var row in rows)
        {
            row.Errors.Clear();
            row.Warnings.Clear();
            row.IsDuplicateInFile = false;
            row.IsDuplicateInDatabase = false;
        }

        // Re-validate
        var validatedRows = await ValidateRowsAsync(rows, clubId, cancellationToken);

        // Create result with empty column mappings (not relevant for re-validation)
        return CreateResult(validatedRows, []);
    }

    private static DataTable ReadCsvDataTable(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true
        };

        parser.SetDelimiters(",");

        var table = new DataTable();

        if (parser.EndOfData)
        {
            return table;
        }

        var headers = parser.ReadFields() ?? [];
        foreach (var header in headers)
        {
            var baseName = header?.Trim() ?? string.Empty;
            var columnName = baseName;
            var index = 1;

            while (table.Columns.Contains(columnName))
            {
                columnName = string.IsNullOrWhiteSpace(baseName)
                    ? $"Column{index}"
                    : $"{baseName}_{index}";
                index++;
            }

            table.Columns.Add(columnName);
        }

        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = parser.ReadFields();
            if (fields is null)
            {
                continue;
            }

            var row = table.NewRow();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                row[i] = i < fields.Length ? fields[i] ?? string.Empty : string.Empty;
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static List<ColumnMappingResultDto> MapColumns(DataColumnCollection columns)
    {
        var mappings = new List<ColumnMappingResultDto>();
        var detectedFields = new HashSet<string>();

        // Check each column header against our known aliases
        foreach (DataColumn column in columns)
        {
            var matchedField = PlayerImportColumnMapping.FindMatchingField(column.ColumnName);
            if (matchedField is not null)
            {
                detectedFields.Add(matchedField);
            }
        }

        // Build mapping results for required fields
        foreach (var fieldName in PlayerImportColumnMapping.RequiredFields.Keys)
        {
            var detected = detectedFields.Contains(fieldName);
            var detectedColumnName = detected ? FindColumnNameForField(columns, fieldName) : null;
            mappings.Add(new ColumnMappingResultDto(fieldName, detectedColumnName, IsRequired: true, detected));
        }

        // Build mapping results for optional fields
        foreach (var fieldName in PlayerImportColumnMapping.OptionalFields.Keys)
        {
            var detected = detectedFields.Contains(fieldName);
            var detectedColumnName = detected ? FindColumnNameForField(columns, fieldName) : null;
            mappings.Add(new ColumnMappingResultDto(fieldName, detectedColumnName, IsRequired: false, detected));
        }

        return mappings;
    }

    private static string? FindColumnNameForField(DataColumnCollection columns, string fieldName)
    {
        if (!PlayerImportColumnMapping.AllFields.TryGetValue(fieldName, out var aliases))
        {
            return null;
        }

        foreach (DataColumn column in columns)
        {
            var normalized = column.ColumnName.Trim().ToLowerInvariant();
            if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                return column.ColumnName;
            }
        }

        return null;
    }

    private static Dictionary<string, int> CreateColumnIndexMap(DataColumnCollection columns, List<ColumnMappingResultDto> mappings)
    {
        var indexMap = new Dictionary<string, int>();

        foreach (var mapping in mappings.Where(m => m.IsDetected && m.DetectedColumnName is not null))
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].ColumnName.Equals(mapping.DetectedColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    indexMap[mapping.FieldName] = i;
                    break;
                }
            }
        }

        return indexMap;
    }

    private static List<PlayerImportRowDto> ParseRows(DataTable table, Dictionary<string, int> columnIndexMap)
    {
        var rows = new List<PlayerImportRowDto>();

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var dataRow = table.Rows[i];
            var row = new PlayerImportRowDto
            {
                RowNumber = rows.Count + 1, // Sequential 1-based index for UI display
                                            // Parse required fields
                FirstName = GetStringValue(dataRow, columnIndexMap, "FirstName"),
                LastName = GetStringValue(dataRow, columnIndexMap, "LastName"),
                DateOfBirth = GetDateValue(dataRow, columnIndexMap, "DateOfBirth"),
                Gender = GetGenderValue(dataRow, columnIndexMap, "Gender"),

                // Parse optional fields
                GraduationYear = GetIntValue(dataRow, columnIndexMap, "GraduationYear"),
                JerseyNumber = GetIntValue(dataRow, columnIndexMap, "JerseyNumber"),
                TryoutNumber = GetIntValue(dataRow, columnIndexMap, "TryoutNumber")
            };

            // Skip empty rows (all key fields are empty)
            var isEmptyRow = string.IsNullOrWhiteSpace(row.FirstName)
                && string.IsNullOrWhiteSpace(row.LastName)
                && !row.DateOfBirth.HasValue
                && !row.Gender.HasValue;

            if (isEmptyRow)
            {
                continue;
            }

            // Compute graduation year if not provided but DOB is available
            if (!row.GraduationYear.HasValue && row.DateOfBirth.HasValue)
            {
                row.GraduationYear = GraduationYearCalculator.ComputeFromDateOfBirth(row.DateOfBirth.Value);
                row.IsGraduationYearComputed = true;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string GetStringValue(DataRow row, Dictionary<string, int> columnMap, string fieldName)
    {
        if (!columnMap.TryGetValue(fieldName, out var index))
        {
            return string.Empty;
        }

        var value = row[index];
        return value == DBNull.Value ? string.Empty : value.ToString()?.Trim() ?? string.Empty;
    }

    private static DateOnly? GetDateValue(DataRow row, Dictionary<string, int> columnMap, string fieldName)
    {
        if (!columnMap.TryGetValue(fieldName, out var index))
        {
            return null;
        }

        var value = row[index];
        if (value == DBNull.Value)
        {
            return null;
        }

        // Handle DateTime values that may already be parsed
        if (value is DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }

        var stringValue = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(stringValue))
        {
            return null;
        }

        // Try parsing with various formats
        foreach (var format in DateFormats)
        {
            if (DateOnly.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Try general parse as fallback
        if (DateOnly.TryParse(stringValue, out var generalDate))
        {
            return generalDate;
        }

        return null;
    }

    private static int? GetIntValue(DataRow row, Dictionary<string, int> columnMap, string fieldName)
    {
        if (!columnMap.TryGetValue(fieldName, out var index))
        {
            return null;
        }

        var value = row[index];
        if (value == DBNull.Value)
        {
            return null;
        }

        // Handle numeric types that may already be parsed
        if (value is double d)
        {
            return (int)d;
        }

        if (value is int i)
        {
            return i;
        }

        var stringValue = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(stringValue))
        {
            return null;
        }

        return int.TryParse(stringValue, out var result) ? result : null;
    }

    private static Gender? GetGenderValue(DataRow row, Dictionary<string, int> columnMap, string fieldName)
    {
        if (!columnMap.TryGetValue(fieldName, out var index))
        {
            return null;
        }

        var value = row[index];
        if (value == DBNull.Value)
        {
            return null;
        }

        var stringValue = value.ToString()?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(stringValue))
        {
            return null;
        }

        return stringValue switch
        {
            "M" or "MALE" => Gender.Male,
            "F" or "FEMALE" => Gender.Female,
            "O" or "OTHER" => Gender.Other,
            _ => null
        };
    }

    private async Task<List<PlayerImportRowDto>> ValidateRowsAsync(
        List<PlayerImportRowDto> rows,
        long clubId,
        CancellationToken cancellationToken)
    {
        // Validate required fields for each row
        foreach (var row in rows)
        {
            ValidateRequiredFields(row);
        }

        // Check for in-file duplicates (same name + DOB)
        CheckInFileDuplicates(rows);

        // Check for database duplicates
        await CheckDatabaseDuplicatesAsync(rows, clubId, cancellationToken);

        // Add warnings for computed graduation years
        foreach (var row in rows.Where(r => r.IsGraduationYearComputed))
        {
            row.Warnings.Add($"Graduation year computed as {row.GraduationYear} based on date of birth.");
        }

        return rows;
    }

    private static void ValidateRequiredFields(PlayerImportRowDto row)
    {
        if (string.IsNullOrWhiteSpace(row.FirstName))
        {
            row.Errors.Add("First name is required.");
        }
        else if (row.FirstName.Length > 100)
        {
            row.Errors.Add("First name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(row.LastName))
        {
            row.Errors.Add("Last name is required.");
        }
        else if (row.LastName.Length > 100)
        {
            row.Errors.Add("Last name must be 100 characters or less.");
        }

        if (!row.DateOfBirth.HasValue)
        {
            row.Errors.Add("Date of birth is required and must be a valid date.");
        }

        if (!row.Gender.HasValue)
        {
            row.Errors.Add("Gender is required. Use M/Male, F/Female, or O/Other.");
        }

        if (!row.GraduationYear.HasValue)
        {
            row.Errors.Add("Graduation year is required (or provide date of birth for automatic calculation).");
        }
        else if (row.GraduationYear < 2000 || row.GraduationYear > DateTime.Now.Year + 25)
        {
            row.Errors.Add($"Graduation year must be between 2000 and {DateTime.Now.Year + 25}.");
        }

        if (row.JerseyNumber.HasValue && (row.JerseyNumber < 0 || row.JerseyNumber > 999))
        {
            row.Errors.Add("Jersey number must be between 0 and 999.");
        }

        if (row.TryoutNumber.HasValue && (row.TryoutNumber < 0 || row.TryoutNumber > 9999))
        {
            row.Errors.Add("Tryout number must be between 0 and 9999.");
        }
    }

    private static void CheckInFileDuplicates(List<PlayerImportRowDto> rows)
    {
        var seen = new Dictionary<string, int>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FirstName) ||
                string.IsNullOrWhiteSpace(row.LastName) ||
                !row.DateOfBirth.HasValue)
            {
                continue;
            }

            var key = $"{row.FirstName.ToUpperInvariant()}|{row.LastName.ToUpperInvariant()}|{row.DateOfBirth:yyyy-MM-dd}";

            if (seen.TryGetValue(key, out var firstRowNumber))
            {
                row.IsDuplicateInFile = true;
                row.Warnings.Add($"Duplicate of row {firstRowNumber} (same name and date of birth).");
            }
            else
            {
                seen[key] = row.RowNumber;
            }
        }
    }

    private async Task CheckDatabaseDuplicatesAsync(
        List<PlayerImportRowDto> rows,
        long clubId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await readOnlyDbContextFactory.CreateDbContextAsync(cancellationToken);

        // Get all existing players for the club (filtered by global query filter)
        var existingPlayers = await dbContext.Players
            .Where(p => p.ClubId == clubId)
            .Select(p => new { p.FirstName, p.LastName, p.DateOfBirth })
            .ToListAsync(cancellationToken);

        var existingSet = existingPlayers
            .Select(p => $"{p.FirstName.ToUpperInvariant()}|{p.LastName.ToUpperInvariant()}|{p.DateOfBirth:yyyy-MM-dd}")
            .ToHashSet();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.FirstName) ||
                string.IsNullOrWhiteSpace(row.LastName) ||
                !row.DateOfBirth.HasValue)
            {
                continue;
            }

            var key = $"{row.FirstName.ToUpperInvariant()}|{row.LastName.ToUpperInvariant()}|{row.DateOfBirth:yyyy-MM-dd}";

            if (existingSet.Contains(key))
            {
                row.IsDuplicateInDatabase = true;
                row.Warnings.Add("A player with the same name and date of birth already exists in this club.");
            }
        }
    }

    private static BulkValidateResultDto CreateResult(List<PlayerImportRowDto> rows, List<ColumnMappingResultDto> columnMappings)
    {
        var validCount = rows.Count(r => r.IsValid);
        var errorCount = rows.Count(r => !r.IsValid);
        var warningCount = rows.Count(r => r.Warnings.Count > 0);
        var duplicateInFileCount = rows.Count(r => r.IsDuplicateInFile);
        var duplicateInDbCount = rows.Count(r => r.IsDuplicateInDatabase);

        // By default, mark valid rows for import
        foreach (var row in rows)
        {
            row.IsMarkedForImport = row.IsValid;
        }

        return new BulkValidateResultDto(
            Rows: rows,
            ColumnMappings: columnMappings,
            MissingRequiredColumns: [],
            ValidCount: validCount,
            ErrorCount: errorCount,
            WarningCount: warningCount,
            DuplicateInFileCount: duplicateInFileCount,
            DuplicateInDatabaseCount: duplicateInDbCount);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Missing required columns in import file: {Columns}. File: {FileName}")]
    private static partial void LogMissingRequiredColumns(ILogger logger, string columns, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Import file parsed successfully. File: {FileName}, Valid: {ValidCount}, Errors: {ErrorCount}, Warnings: {WarningCount}")]
    private static partial void LogParseComplete(ILogger logger, string fileName, int validCount, int errorCount, int warningCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse import file: {FileName}. Error: {Error}")]
    private static partial void LogParseError(ILogger logger, string fileName, string error);
}
