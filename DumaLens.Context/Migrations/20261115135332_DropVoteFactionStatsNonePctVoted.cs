using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class DropVoteFactionStatsNonePctVoted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VoteFactionStats_FactionId_NonePctVoted",
                table: "VoteFactionStats");

            migrationBuilder.DropColumn(
                name: "NonePctVoted",
                table: "VoteFactionStats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NonePctVoted",
                table: "VoteFactionStats",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_NonePctVoted",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "NonePctVoted" });
        }
    }
}