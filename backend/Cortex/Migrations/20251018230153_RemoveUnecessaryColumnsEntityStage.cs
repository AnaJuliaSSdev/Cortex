using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnecessaryColumnsEntityStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stages_AnalysisId_Order",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "PartialResult",
                table: "Stages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Stages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PartialResult",
                table: "Stages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stages_AnalysisId_Order",
                table: "Stages",
                columns: new[] { "AnalysisId", "Order" });
        }
    }
}
