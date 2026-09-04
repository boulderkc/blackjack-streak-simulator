using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackjackStreakSimulator.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToSingular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LowestBankrollBucketEntry_BatchHistories_BatchHistoryEntryId",
                table: "LowestBankrollBucketEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_StreakLengthFrequencyEntry_BatchHistories_BatchHistoryEntryId",
                table: "StreakLengthFrequencyEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BatchHistories",
                table: "BatchHistories");

            migrationBuilder.RenameTable(
                name: "BatchHistories",
                newName: "BatchHistoryEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BatchHistoryEntry",
                table: "BatchHistoryEntry",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LowestBankrollBucketEntry_BatchHistoryEntry_BatchHistoryEntryId",
                table: "LowestBankrollBucketEntry",
                column: "BatchHistoryEntryId",
                principalTable: "BatchHistoryEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StreakLengthFrequencyEntry_BatchHistoryEntry_BatchHistoryEntryId",
                table: "StreakLengthFrequencyEntry",
                column: "BatchHistoryEntryId",
                principalTable: "BatchHistoryEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LowestBankrollBucketEntry_BatchHistoryEntry_BatchHistoryEntryId",
                table: "LowestBankrollBucketEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_StreakLengthFrequencyEntry_BatchHistoryEntry_BatchHistoryEntryId",
                table: "StreakLengthFrequencyEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BatchHistoryEntry",
                table: "BatchHistoryEntry");

            migrationBuilder.RenameTable(
                name: "BatchHistoryEntry",
                newName: "BatchHistories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BatchHistories",
                table: "BatchHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LowestBankrollBucketEntry_BatchHistories_BatchHistoryEntryId",
                table: "LowestBankrollBucketEntry",
                column: "BatchHistoryEntryId",
                principalTable: "BatchHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StreakLengthFrequencyEntry_BatchHistories_BatchHistoryEntryId",
                table: "StreakLengthFrequencyEntry",
                column: "BatchHistoryEntryId",
                principalTable: "BatchHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
