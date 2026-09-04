using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackjackStreakSimulator.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialBankroll = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseBet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BankrollGoal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DecksInShoe = table.Column<int>(type: "int", nullable: false),
                    MaxStreakCount = table.Column<int>(type: "int", nullable: false),
                    SeatCount = table.Column<int>(type: "int", nullable: false),
                    BettingMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    TimesReachedGoal = table.Column<int>(type: "int", nullable: false),
                    TimesBusted = table.Column<int>(type: "int", nullable: false),
                    AverageHandsPlayedWhenReachedGoal = table.Column<double>(type: "float", nullable: false),
                    AverageHandsPlayedWhenBusted = table.Column<double>(type: "float", nullable: false),
                    AverageMaxDrawdownWhenReachedGoal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorstMaxDrawdownWhenReachedGoal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LowestBankrollBucketEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchHistoryEntryId = table.Column<int>(type: "int", nullable: false),
                    BucketFloor = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LowestBankrollBucketEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LowestBankrollBucketEntry_BatchHistories_BatchHistoryEntryId",
                        column: x => x.BatchHistoryEntryId,
                        principalTable: "BatchHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreakLengthFrequencyEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchHistoryEntryId = table.Column<int>(type: "int", nullable: false),
                    StreakLength = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreakLengthFrequencyEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreakLengthFrequencyEntry_BatchHistories_BatchHistoryEntryId",
                        column: x => x.BatchHistoryEntryId,
                        principalTable: "BatchHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LowestBankrollBucketEntry_BatchHistoryEntryId",
                table: "LowestBankrollBucketEntry",
                column: "BatchHistoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_StreakLengthFrequencyEntry_BatchHistoryEntryId",
                table: "StreakLengthFrequencyEntry",
                column: "BatchHistoryEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LowestBankrollBucketEntry");

            migrationBuilder.DropTable(
                name: "StreakLengthFrequencyEntry");

            migrationBuilder.DropTable(
                name: "BatchHistories");
        }
    }
}
