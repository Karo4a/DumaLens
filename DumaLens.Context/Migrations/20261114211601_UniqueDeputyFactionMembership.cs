using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDeputyFactionMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeputyFactionMembership_DeputyId_FactionPeriodId",
                table: "DeputyFactionMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyFactionMembership_DeputyId_FactionPeriodId",
                table: "DeputyFactionMemberships",
                columns: new[] { "DeputyId", "FactionPeriodId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeputyFactionMembership_DeputyId_FactionPeriodId",
                table: "DeputyFactionMemberships");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyFactionMembership_DeputyId_FactionPeriodId",
                table: "DeputyFactionMemberships",
                columns: new[] { "DeputyId", "FactionPeriodId" });
        }
    }
}