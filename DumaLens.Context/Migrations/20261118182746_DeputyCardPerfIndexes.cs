using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class DeputyCardPerfIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeputyVote_DeputyId",
                table: "DeputyVotes");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_Deleted_RowId",
                table: "Votes",
                column: "RowId",
                filter: "\"DeletedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyVote_DeputyId",
                table: "DeputyVotes",
                columns: new[] { "DeputyId", "Result", "VoteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vote_Deleted_RowId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_DeputyVote_DeputyId",
                table: "DeputyVotes");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyVote_DeputyId",
                table: "DeputyVotes",
                column: "DeputyId");
        }
    }
}