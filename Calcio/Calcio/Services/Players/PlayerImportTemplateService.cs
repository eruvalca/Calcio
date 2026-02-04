using System.Text;

using Calcio.Shared.Services.Players;
using Calcio.Shared.Validation;

using ClosedXML.Excel;

namespace Calcio.Services.Players;

/// <summary>
/// Service for generating player import templates using ClosedXML.
/// </summary>
public class PlayerImportTemplateService : IPlayerImportTemplateService
{
    private static readonly string[] SampleData =
    [
        "John",      // First Name
        "Doe",       // Last Name
        "2010-05-15", // Date of Birth
        "M",         // Gender
        "2028",      // Graduation Year
        "10",        // Jersey Number
        ""           // Tryout Number (optional)
    ];

    public byte[] GenerateCsvTemplate()
    {
        var sb = new StringBuilder();

        // Header row using template headers
        sb.AppendLine(string.Join(",", PlayerImportColumnMapping.TemplateHeaders));

        // Sample data row
        sb.AppendLine(string.Join(",", SampleData));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GenerateExcelTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Players");

        // Header row with display-friendly names
        for (var i = 0; i < PlayerImportColumnMapping.TemplateDisplayHeaders.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = PlayerImportColumnMapping.TemplateDisplayHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Sample data row
        for (var i = 0; i < SampleData.Length; i++)
        {
            worksheet.Cell(2, i + 1).Value = SampleData[i];
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add data validation for Gender column (column 4)
        var genderColumn = worksheet.Range(2, 4, 1000, 4);
        genderColumn.CreateDataValidation().List("M,F,O", true);

        // Add comments/notes for guidance
        worksheet.Cell(1, 1).CreateComment().AddText("Required: Player's first name");
        worksheet.Cell(1, 2).CreateComment().AddText("Required: Player's last name");
        worksheet.Cell(1, 3).CreateComment().AddText("Required: Format YYYY-MM-DD or M/D/YYYY");
        worksheet.Cell(1, 4).CreateComment().AddText("Required: M (Male), F (Female), or O (Other)");
        worksheet.Cell(1, 5).CreateComment().AddText("Optional: Will be calculated from DOB if not provided");
        worksheet.Cell(1, 6).CreateComment().AddText("Optional: 0-999");
        worksheet.Cell(1, 7).CreateComment().AddText("Optional: 0-9999");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
