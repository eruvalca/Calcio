using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calcio.Data.Migrations;

/// <inheritdoc />
public partial class MakeGraduationYearRequired : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "GraduationYear",
            table: "Teams",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "GraduationYear",
            table: "Players",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "GraduationYear",
            table: "Teams",
            type: "integer",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<int>(
            name: "GraduationYear",
            table: "Players",
            type: "integer",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");
    }
}
