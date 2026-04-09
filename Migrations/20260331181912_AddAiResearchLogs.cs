using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldBranchAI.Migrations
{
    /// <inheritdoc />
    public partial class AddAiResearchLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiResearchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    RequestPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiResearchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiResearchLogs_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiResearchLogs_AppUserId",
                table: "AiResearchLogs",
                column: "AppUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiResearchLogs");
        }
    }
}
