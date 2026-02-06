using System.Text;

using Calcio.Shared.Services.Players;
using Calcio.Shared.Validation;

namespace Calcio.Services.Players;

/// <summary>
/// Service for generating player import templates.
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

}
