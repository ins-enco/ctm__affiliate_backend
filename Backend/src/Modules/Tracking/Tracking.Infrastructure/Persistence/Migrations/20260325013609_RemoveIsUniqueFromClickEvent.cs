using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsUniqueFromClickEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUnique",
                table: "click_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUnique",
                table: "click_events",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
