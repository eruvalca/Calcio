using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Calcio.Data.Migrations;

/// <inheritdoc />
public partial class CalcioUserPhoto : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CalcioUserPhotos",
            columns: table => new
            {
                CalcioUserPhotoId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OriginalBlobName = table.Column<string>(type: "text", nullable: false),
                SmallBlobName = table.Column<string>(type: "text", nullable: true),
                MediumBlobName = table.Column<string>(type: "text", nullable: true),
                LargeBlobName = table.Column<string>(type: "text", nullable: true),
                ContentType = table.Column<string>(type: "text", nullable: true),
                CalcioUserId = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedById = table.Column<long>(type: "bigint", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ModifiedById = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalcioUserPhotos", x => x.CalcioUserPhotoId);
                table.ForeignKey(
                    name: "FK_CalcioUserPhotos_AspNetUsers_CalcioUserId",
                    column: x => x.CalcioUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CalcioUserPhotos_CalcioUserId",
            table: "CalcioUserPhotos",
            column: "CalcioUserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CalcioUserPhotos");
    }
}
