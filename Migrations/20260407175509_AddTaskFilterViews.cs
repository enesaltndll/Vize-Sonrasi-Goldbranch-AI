using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldBranchAI.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskFilterViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskFilterViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskFilterViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskFilterViews_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskFilterViews_AppUserId_Name",
                table: "TaskFilterViews",
                columns: new[] { "AppUserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskFilterViews");
        }
    }
}
