using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cortex.Migrations
{
    /// <inheritdoc />
    public partial class AddExplorationStageEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    ExplorationOfMaterialStageId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Stages_ExplorationOfMaterialStageId",
                        column: x => x.ExplorationOfMaterialStageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegisterUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    SourceDocumentUri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Page = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Line = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegisterUnits_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndexRegisterUnit",
                columns: table => new
                {
                    FoundIndicesId = table.Column<int>(type: "integer", nullable: false),
                    RegisterUnitsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexRegisterUnit", x => new { x.FoundIndicesId, x.RegisterUnitsId });
                    table.ForeignKey(
                        name: "FK_IndexRegisterUnit_Indexes_FoundIndicesId",
                        column: x => x.FoundIndicesId,
                        principalTable: "Indexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndexRegisterUnit_RegisterUnits_RegisterUnitsId",
                        column: x => x.RegisterUnitsId,
                        principalTable: "RegisterUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ExplorationOfMaterialStageId_Name",
                table: "Categories",
                columns: new[] { "ExplorationOfMaterialStageId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndexRegisterUnit_RegisterUnitsId",
                table: "IndexRegisterUnit",
                column: "RegisterUnitsId");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterUnits_CategoryId",
                table: "RegisterUnits",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexRegisterUnit");

            migrationBuilder.DropTable(
                name: "RegisterUnits");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
