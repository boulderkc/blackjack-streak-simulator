using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackjackStreakSimulator.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBustedToBankrupt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimesBusted",
                table: "BatchHistoryEntry",
                newName: "TimesBankrupt");

            migrationBuilder.RenameColumn(
                name: "AverageHandsPlayedWhenBusted",
                table: "BatchHistoryEntry",
                newName: "AverageHandsPlayedWhenBankrupt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimesBankrupt",
                table: "BatchHistoryEntry",
                newName: "TimesBusted");

            migrationBuilder.RenameColumn(
                name: "AverageHandsPlayedWhenBankrupt",
                table: "BatchHistoryEntry",
                newName: "AverageHandsPlayedWhenBusted");
        }
    }
}
