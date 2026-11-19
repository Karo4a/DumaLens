using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class AuthSessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_EmailLowerCase",
                table: "Users");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFailedLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_User_EmailLowerCase",
                table: "Users",
                column: "EmailLowerCase",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Email\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_EmailLowerCase",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_User_EmailLowerCase",
                table: "Users",
                column: "EmailLowerCase",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}