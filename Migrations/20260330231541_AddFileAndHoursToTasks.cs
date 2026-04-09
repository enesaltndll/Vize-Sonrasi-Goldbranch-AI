using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldBranchAI.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAndHoursToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedTimeMinutes",
                table: "Tasks",
                newName: "EstimatedTimeHours");

            migrationBuilder.AddColumn<string>(
                name: "AttachedFilePath",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachedFilePath",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "EstimatedTimeHours",
                table: "Tasks",
                newName: "EstimatedTimeMinutes");
        }
    }
}
