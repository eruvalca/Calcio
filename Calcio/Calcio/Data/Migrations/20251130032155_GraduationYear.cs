using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calcio.Data.Migrations;

/// <inheritdoc />
public partial class GraduationYear : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "BirthYear",
            table: "Teams",
            newName: "GraduationYear");

        migrationBuilder.AddColumn<int>(
            name: "GraduationYear",
            table: "Players",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GraduationYear",
            table: "Players");

        migrationBuilder.RenameColumn(
            name: "GraduationYear",
            table: "Teams",
            newName: "BirthYear");
    }
}
