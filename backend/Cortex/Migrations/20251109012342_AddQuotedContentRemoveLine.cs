using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotedContentRemoveLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Line",
                table: "RegisterUnits");

            migrationBuilder.DropColumn(
                name: "Line",
                table: "IndexReferences");

            migrationBuilder.AlterColumn<string>(
                name: "QuotedContent",
                table: "IndexReferences",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Line",
                table: "RegisterUnits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "QuotedContent",
                table: "IndexReferences",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Line",
                table: "IndexReferences",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
