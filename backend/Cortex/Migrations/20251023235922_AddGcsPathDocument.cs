using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Migrations
{
    /// <inheritdoc />
    public partial class AddGcsPathDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GcsFilePath",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GcsFilePath",
                table: "Documents");
        }
    }
}
