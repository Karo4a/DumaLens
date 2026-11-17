using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class DropRowIdDuplicateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vote_RowId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_FactionPeriod_RowId",
                table: "FactionPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Deputy_RowId",
                table: "Deputies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vote_RowId",
                table: "Votes",
                column: "RowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionPeriod_RowId",
                table: "FactionPeriods",
                column: "RowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deputy_RowId",
                table: "Deputies",
                column: "RowId",
                unique: true);
        }
    }
}