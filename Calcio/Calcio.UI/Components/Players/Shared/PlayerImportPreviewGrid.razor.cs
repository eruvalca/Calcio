using Calcio.Shared.DTOs.Players.BulkImport;
using Calcio.Shared.Extensions;

using Microsoft.AspNetCore.Components;

namespace Calcio.UI.Components.Players.Shared;

public partial class PlayerImportPreviewGrid
{
    [Parameter]
    public required List<PlayerImportRowDto> Rows { get; set; }

    [Parameter]
    public EventCallback<List<PlayerImportRowDto>> RowsChanged { get; set; }

    [Parameter]
    public EventCallback OnRowsModified { get; set; }

    private void OnRowSelectionChanged(PlayerImportRowDto row, bool isSelected)
    {
        row.IsMarkedForImport = isSelected;
        OnRowsModified.InvokeAsync();
    }

    private void OnDateOfBirthChanged(PlayerImportRowDto row, string? dateString)
    {
        if (DateOnly.TryParse(dateString, out var date))
        {
            row.DateOfBirth = date;

            // Recompute graduation year if it was computed or not set
            if (row.IsGraduationYearComputed || !row.GraduationYear.HasValue)
            {
                row.GraduationYear = GraduationYearCalculator.ComputeFromDateOfBirth(date);
                row.IsGraduationYearComputed = true;

                // Update warning
                var existingWarning = row.Warnings.FirstOrDefault(w => w.StartsWith("Graduation year computed"));
                if (existingWarning is not null)
                {
                    row.Warnings.Remove(existingWarning);
                }

                row.Warnings.Add($"Graduation year computed as {row.GraduationYear} based on date of birth.");
            }
        }
        else
        {
            row.DateOfBirth = null;
        }

        ValidateRow(row);
    }

    private void ValidateRow(PlayerImportRowDto row)
    {
        // Clear previous errors (but keep warnings for duplicates which require server re-validation)
        row.Errors.Clear();

        // Validate required fields locally
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
            row.Errors.Add("Date of birth is required.");
        }

        if (!row.Gender.HasValue)
        {
            row.Errors.Add("Gender is required.");
        }

        if (!row.GraduationYear.HasValue)
        {
            row.Errors.Add("Graduation year is required.");
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

        // Update selection if row becomes invalid
        if (!row.IsValid)
        {
            row.IsMarkedForImport = false;
        }

        OnRowsModified.InvokeAsync();
    }

    private void RemoveRow(PlayerImportRowDto row)
    {
        Rows.Remove(row);
        RowsChanged.InvokeAsync(Rows);
        OnRowsModified.InvokeAsync();
    }

    private static bool HasFieldError(PlayerImportRowDto row, string fieldPrefix)
        => row.Errors.Any(e => e.StartsWith(fieldPrefix, StringComparison.OrdinalIgnoreCase));
}
