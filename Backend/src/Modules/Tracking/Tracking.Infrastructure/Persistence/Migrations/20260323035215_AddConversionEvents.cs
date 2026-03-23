using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversion_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AffiliateId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ConversionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConvertedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversion_events", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_AffiliateId_ConversionType",
                table: "conversion_events",
                columns: new[] { "AffiliateId", "ConversionType" });

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_ConvertedAt",
                table: "conversion_events",
                column: "ConvertedAt");

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_SessionId",
                table: "conversion_events",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_SessionId_ConversionType",
                table: "conversion_events",
                columns: new[] { "SessionId", "ConversionType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversion_events");
        }
    }
}
