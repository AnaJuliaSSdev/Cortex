using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Documents");

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Documents",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
