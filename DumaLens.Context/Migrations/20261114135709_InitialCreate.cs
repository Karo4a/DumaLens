using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;
using System;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Convocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    SiteCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deputies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SortName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deputies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportQuarantine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFile = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SiteId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportQuarantine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UtcOffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StageRaw = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AcceptCountTotal = table.Column<int>(type: "integer", nullable: false),
                    DeclineCountTotal = table.Column<int>(type: "integer", nullable: false),
                    AbstainCountTotal = table.Column<int>(type: "integer", nullable: false),
                    NoneCountTotal = table.Column<int>(type: "integer", nullable: false),
                    AcceptPercentSite = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HasResults = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TitleSearch = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('russian'::regconfig, \"Title\")", stored: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votes_Convocations_ConvocationId",
                        column: x => x.ConvocationId,
                        principalTable: "Convocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FactionPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionPeriods_Convocations_ConvocationId",
                        column: x => x.ConvocationId,
                        principalTable: "Convocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionPeriods_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LoginLowerCase = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmailLowerCase = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    PasswordSalt = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: false),
                    PasswordIsTemporary = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Blocked = table.Column<bool>(type: "boolean", nullable: false),
                    LoginAttemptFailedCount = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeputyFactionMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeputyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeputyFactionMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeputyFactionMemberships_Deputies_DeputyId",
                        column: x => x.DeputyId,
                        principalTable: "Deputies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeputyFactionMemberships_FactionPeriods_FactionPeriodId",
                        column: x => x.FactionPeriodId,
                        principalTable: "FactionPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeputyVotes",
                columns: table => new
                {
                    VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeputyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionPeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                    Result = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeputyVotes", x => new { x.VoteId, x.DeputyId });
                    table.ForeignKey(
                        name: "FK_DeputyVotes_Deputies_DeputyId",
                        column: x => x.DeputyId,
                        principalTable: "Deputies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeputyVotes_FactionPeriods_FactionPeriodId",
                        column: x => x.FactionPeriodId,
                        principalTable: "FactionPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeputyVotes_Votes_VoteId",
                        column: x => x.VoteId,
                        principalTable: "Votes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoteFactionStats",
                columns: table => new
                {
                    VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptCount = table.Column<int>(type: "integer", nullable: false),
                    DeclineCount = table.Column<int>(type: "integer", nullable: false),
                    AbstainCount = table.Column<int>(type: "integer", nullable: false),
                    NoneCount = table.Column<int>(type: "integer", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    VotedCount = table.Column<int>(type: "integer", nullable: false),
                    AcceptPctMembers = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DeclinePctMembers = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AbstainPctMembers = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    NonePctMembers = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AcceptPctVoted = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DeclinePctVoted = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AbstainPctVoted = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    NonePctVoted = table.Column<decimal>(type: "numeric(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteFactionStats", x => new { x.VoteId, x.FactionPeriodId });
                    table.ForeignKey(
                        name: "FK_VoteFactionStats_FactionPeriods_FactionPeriodId",
                        column: x => x.FactionPeriodId,
                        principalTable: "FactionPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteFactionStats_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteFactionStats_Votes_VoteId",
                        column: x => x.VoteId,
                        principalTable: "Votes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Convocations",
                columns: new[] { "Id", "CreatedAt", "DateFrom", "DateTo", "DeletedAt", "Number", "SiteCode", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, 7, "AAAAAAA7", "VII созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, 1, "AAAAAAA1", "I созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("56d58d7a-467f-48be-b236-42b4d133fd02"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2011, 12, 21), new DateOnly(2016, 10, 4), null, 6, "AAAAAAA6", "VI созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, 2, "AAAAAAA2", "II созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, 4, "AAAAAAA4", "IV созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, 3, "AAAAAAA3", "III созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2008, 1, 1), new DateOnly(2011, 12, 20), null, 5, "AAAAAAA5", "V созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f7e48a00-05ce-43d3-b7e4-bf206ff43efa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2026, 9, 30), null, null, 9, "AAAAAAA9", "IX созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, 8, "AAAAAAA8", "VIII созыв", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Factions",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2669c56a-cff0-4135-95a6-1b4e1696674b"), "NRDV", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "НРДВ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("2a6e744b-7858-4e83-a3d8-7d1dc63a03ad"), "RREG", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "РРег", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("315fdc5b-11ad-407e-a45d-1cb778bd822d"), "APG", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "АПГ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("51cf331e-c029-4e16-9eec-affba832201e"), "SR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Справедливая Россия", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("56dace41-92af-49d2-b6c7-622b13bd4fbc"), "STAB", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Стаб", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("58c90289-b686-4eea-9479-c605be93b21a"), "LDS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ЛДС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), "KPRF", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("68fe8a83-5d8b-4b26-8312-1957075bde79"), "YABLOKO", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ЯБЛ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6d678469-8ac9-42b5-bffb-615937bc14fb"), "ROSS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Росс", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("81264f05-daf0-4b91-9ba8-381f31404362"), "VR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ВР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("845e78af-e7f2-4bdb-a014-f63ca4eebfd8"), "AGR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Агр", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("904b8d63-2500-435c-87f1-3d5467989207"), "SPS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "СПС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("92d7276d-bea2-4751-a4c1-7f8c0ee118d4"), "NL", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Новые люди", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b5265678-a153-4ade-9010-022275aabac6"), "LDPR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c87fb255-1817-441e-a700-406fc2b6df61"), "DPR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ce75e7e7-78cc-4280-89f8-833d0346f7ac"), "NDR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "НДР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ceeb9b8d-12e6-469c-940f-fa04152e633e"), "RROS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "РРос", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d042a040-8020-406c-91db-944e0efa5f74"), "INDEPENDENT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d77c1470-3098-4e22-8f7e-29866bb6a976"), "NDEP", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "НДеп", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dd3b261e-94c8-4827-9855-2f0d345e0b64"), "RNVS", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "РНВС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("df00511a-7978-4289-8fb8-da47a4ef80b1"), "ZHR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ЖР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e768d23a-f9ee-4974-bd59-b667d0d49206"), "N96", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Н-96", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e97addca-c253-4ff8-b3eb-2e6a41491221"), "APR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "АПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ebb9199b-c439-4c62-bc71-72eab85f2019"), "PRES", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "ПРЕС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), "ER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Единая Россия", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("658ab516-79a2-49ae-afec-98a9b2ca35e2"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Ведёт справочники и теги: права пользователя + модерация тегов (v1.1), правка нормализации", "Moderator", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("781c1977-48c9-43ff-9091-6ed35bf720ed"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Зарегистрированный читатель: публичные данные, профиль, оценки важности (v1.2)", "User", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f8a38ae3-c73e-4751-91ad-856e29c0df4c"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Управляет системой: права модератора + пользователи и роли, блокировка, сброс паролей, служебные операции", "Administrator", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "FactionPeriods",
                columns: new[] { "Id", "ConvocationId", "CreatedAt", "DateFrom", "DateTo", "DeletedAt", "FactionId", "SiteId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("037fba2c-4821-4f0c-b118-191702aa102b"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("315fdc5b-11ad-407e-a45d-1cb778bd822d"), 72100021L, "АПГ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("03b2975f-0c6d-4117-ae9c-6437713e4cf7"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("51cf331e-c029-4e16-9eec-affba832201e"), 72100029L, "СР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("097aeacc-55c4-4c00-a595-daf65eed644b"), new Guid("56d58d7a-467f-48be-b236-42b4d133fd02"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2011, 12, 21), new DateOnly(2016, 10, 4), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100024L, "ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0a3c565b-ad02-447c-a9be-9eb10f70c354"), new Guid("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2008, 1, 1), new DateOnly(2011, 12, 20), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("0d61b8d3-3374-49e9-8dec-fe9d887ee1e2"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("6d678469-8ac9-42b5-bffb-615937bc14fb"), 72100013L, "Росс", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("13d93636-ebe0-48c1-8caf-a1644038f060"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("904b8d63-2500-435c-87f1-3d5467989207"), 72100015L, "СПС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("1782447e-910d-42f9-ab92-cf47dfffd09e"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("18edbd04-2265-457f-9e8b-0a17377a26aa"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("81264f05-daf0-4b91-9ba8-381f31404362"), 72100002L, "ВР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("1a15de28-511d-4807-9d47-361ae282559a"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("2da0ebe0-a68b-4a57-9506-a766548db824"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("dd3b261e-94c8-4827-9855-2f0d345e0b64"), 72100026L, "РНВС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("2e323136-978d-4ae6-bded-ae56b10c95c4"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3064d9e6-d9fe-46b7-9878-466b1cb70f1d"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100024L, "ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("360aef12-a5e7-4876-8080-791474fe2683"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("92d7276d-bea2-4751-a4c1-7f8c0ee118d4"), 72100028L, "НЛ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3d299bfd-86ab-40aa-a7a3-feb41ed35835"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("56dace41-92af-49d2-b6c7-622b13bd4fbc"), 72100012L, "Стаб", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3e57a969-8b4c-4bc0-b780-4164d8fdfafb"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("58c90289-b686-4eea-9479-c605be93b21a"), 72100016L, "ЛДС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3f1e4c94-4a1b-4b5c-9db6-0a095b12d960"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("45fc0012-037f-4ef7-86b2-e5373b73e60e"), new Guid("56d58d7a-467f-48be-b236-42b4d133fd02"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2011, 12, 21), new DateOnly(2016, 10, 4), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("479075db-b7f5-459f-9009-588d946fb4d1"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("2669c56a-cff0-4135-95a6-1b4e1696674b"), 72100019L, "НРДВ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("525de271-d6b5-4cc6-97c9-68160fe063a6"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("57b2cb7c-c086-4b0a-a651-1d25d2e8bf51"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("d77c1470-3098-4e22-8f7e-29866bb6a976"), 72100022L, "НДеп", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("64845a76-f80a-495c-9b7f-2d316b8b08e1"), new Guid("56d58d7a-467f-48be-b236-42b4d133fd02"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2011, 12, 21), new DateOnly(2016, 10, 4), null, new Guid("51cf331e-c029-4e16-9eec-affba832201e"), 72100027L, "СР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6c55d2de-8184-4847-a937-7a5604375e98"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("73aa83b0-4cbd-4d1d-bfd3-817b7584691f"), new Guid("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2008, 1, 1), new DateOnly(2011, 12, 20), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("78fbf9e3-d315-4c9e-b71e-509a58ff7e13"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("83b180c6-e062-4043-92fc-4e195fff8ae2"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("e768d23a-f9ee-4974-bd59-b667d0d49206"), 72100014L, "Н-96", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("92db6383-260b-42d5-b7d2-bb3d55fd3a87"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("51cf331e-c029-4e16-9eec-affba832201e"), 72100025L, "СР-Р", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("95596282-12fd-4831-b70d-2d04c15c5a95"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100009L, "Е-ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("9c8ce4cd-cfaa-45b5-b78f-e1d403bcfeed"), new Guid("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2008, 1, 1), new DateOnly(2011, 12, 20), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100024L, "ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("9dc5165f-df1e-4574-85c5-b4f6f827500c"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("9eca1ed4-0320-4122-8dff-06ecd774f433"), new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("9fad73c0-322a-4b9a-813f-b33163b6f66b"), new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100024L, "ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a583d916-1cca-4532-a7e0-a217b8e854e3"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100010L, "О-ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ac681593-4b27-4736-830b-0485a902f652"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("c87fb255-1817-441e-a700-406fc2b6df61"), 72100003L, "ДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ad275bd5-0e8a-404e-91b8-bce6313aa5f4"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b12d0021-afb4-4d9c-afb4-8ecc1520be9b"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b3140166-5362-42fc-97cb-69766b9cf021"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("845e78af-e7f2-4bdb-a014-f63ca4eebfd8"), 72100018L, "Агр", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b51c99bb-7967-4ea0-a9cf-7f9f2eefa604"), new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, new Guid("51cf331e-c029-4e16-9eec-affba832201e"), 72100027L, "СР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b562bf61-a5ea-4b29-807f-9f9fb68bfd5c"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b949ed05-f648-4847-b220-b24f5f2fbbf7"), new Guid("f93db281-114d-4f32-b4bb-c3394d7423fb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bdf9bb0b-7949-437b-b984-1eac067eaef7"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("ebb9199b-c439-4c62-bc71-72eab85f2019"), 72100006L, "ПРЕС", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ca43b4b7-32eb-4041-af2c-ac5c3fb6fea1"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ce2fc4c1-b08f-464b-a6db-afc84747133a"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("e97addca-c253-4ff8-b3eb-2e6a41491221"), 72100001L, "АПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("de5885e3-2f98-40f7-a81a-5d59ab86c3ea"), new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("de7587b9-4c9b-4841-83fb-0a0ca064c790"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("ce75e7e7-78cc-4280-89f8-833d0346f7ac"), 72100017L, "НДР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("df045fa7-b9cb-4edc-8e8f-b3632dc638c3"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("ceeb9b8d-12e6-469c-940f-fa04152e633e"), 72100023L, "РРос", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e1c57b62-84e1-4ca4-b377-daa0379cb03f"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("d042a040-8020-406c-91db-944e0efa5f74"), 72100011L, "Вне фракций", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e326e66d-30cf-4e9d-a0ce-cff4c9b258c0"), new Guid("c2c92ca1-cb8e-4aed-9e3f-ab00b1b47ae4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2008, 1, 1), new DateOnly(2011, 12, 20), null, new Guid("51cf331e-c029-4e16-9eec-affba832201e"), 72100027L, "СР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e4d51638-bdeb-4587-8eef-7a9acb92f84a"), new Guid("56d58d7a-467f-48be-b236-42b4d133fd02"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2011, 12, 21), new DateOnly(2016, 10, 4), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e6658cd3-6592-4872-b89c-548a00f7ed67"), new Guid("8be015c9-1873-4013-b78d-958798a9b76f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2000, 1, 1), new DateOnly(2003, 12, 31), null, new Guid("68fe8a83-5d8b-4b26-8312-1957075bde79"), 72100008L, "ЯБЛ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e7125771-2a99-4ff2-9fd1-afc68c841c87"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"), 72100004L, "КПРФ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eb123a42-74fa-46d6-9cd9-93b39bbfc5af"), new Guid("78952456-8142-4c5a-b63e-b0cb6dc754ea"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2004, 1, 1), new DateOnly(2007, 12, 31), null, new Guid("f0e605dc-c9c2-4894-8c83-13bf0480e62d"), 72100024L, "ЕР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ee3550f1-a11b-4d54-b1f7-f2f763bf75c4"), new Guid("45af1ff7-eacb-4b06-8529-ff46c1afa5fa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(2016, 10, 5), new DateOnly(2021, 10, 11), null, new Guid("b5265678-a153-4ade-9010-022275aabac6"), 72100005L, "ЛДПР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ee81b4a1-6a69-436d-a48b-6d7d328c5c2b"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("df00511a-7978-4289-8fb8-da47a4ef80b1"), 72100007L, "ЖР", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("fd3ba9c6-d7d9-4381-b955-80b7ab854d59"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("68fe8a83-5d8b-4b26-8312-1957075bde79"), 72100008L, "ЯБЛ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ff5fdbfd-f7c9-401e-a489-a4d844c49050"), new Guid("64e17e07-f6c0-4479-b5d3-795ebb36d0aa"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1996, 1, 1), new DateOnly(1999, 12, 31), null, new Guid("2a6e744b-7858-4e83-a3d8-7d1dc63a03ad"), 72100020L, "РРег", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ff959403-b47e-4f60-bff9-a9d3e24f00d1"), new Guid("519949c4-5bb5-4a77-a963-81ba6fa39ab8"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateOnly(1994, 1, 11), new DateOnly(1995, 12, 31), null, new Guid("68fe8a83-5d8b-4b26-8312-1957075bde79"), 72100008L, "ЯБЛ", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bill_Number",
                table: "Bills",
                column: "Number",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Convocation_Number",
                table: "Convocations",
                column: "Number",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Convocation_SiteCode",
                table: "Convocations",
                column: "SiteCode",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Deputy_FullName",
                table: "Deputies",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Deputy_SiteId",
                table: "Deputies",
                column: "SiteId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyFactionMembership_DeputyId_FactionPeriodId",
                table: "DeputyFactionMemberships",
                columns: new[] { "DeputyId", "FactionPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeputyFactionMemberships_FactionPeriodId",
                table: "DeputyFactionMemberships",
                column: "FactionPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyVote_DeputyId",
                table: "DeputyVotes",
                column: "DeputyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeputyVote_VoteId_FactionPeriodId",
                table: "DeputyVotes",
                columns: new[] { "VoteId", "FactionPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeputyVotes_FactionPeriodId",
                table: "DeputyVotes",
                column: "FactionPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionPeriod_FactionId",
                table: "FactionPeriods",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionPeriod_SiteId_ConvocationId",
                table: "FactionPeriods",
                columns: new[] { "SiteId", "ConvocationId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FactionPeriods_ConvocationId",
                table: "FactionPeriods",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Faction_Code",
                table: "Factions",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportQuarantine_CreatedAt",
                table: "ImportQuarantine",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshTokens",
                column: "UserId",
                unique: true,
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Role",
                table: "Roles",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_EmailLowerCase",
                table: "Users",
                column: "EmailLowerCase",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_LoginLowerCase",
                table: "Users",
                column: "LoginLowerCase",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_AbstainPctMembers",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "AbstainPctMembers" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_AbstainPctVoted",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "AbstainPctVoted" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_AcceptPctMembers",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "AcceptPctMembers" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_AcceptPctVoted",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "AcceptPctVoted" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_DeclinePctMembers",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "DeclinePctMembers" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_DeclinePctVoted",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "DeclinePctVoted" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_NonePctMembers",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "NonePctMembers" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionId_NonePctVoted",
                table: "VoteFactionStats",
                columns: new[] { "FactionId", "NonePctVoted" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteFactionStats_FactionPeriodId",
                table: "VoteFactionStats",
                column: "FactionPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_BillId",
                table: "Votes",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ConvocationId",
                table: "Votes",
                column: "ConvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_ConvocationId_VotedAt",
                table: "Votes",
                columns: new[] { "ConvocationId", "VotedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Vote_SiteId",
                table: "Votes",
                column: "SiteId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_Stage",
                table: "Votes",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_TitleSearch",
                table: "Votes",
                column: "TitleSearch")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_VotedAt",
                table: "Votes",
                column: "VotedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeputyFactionMemberships");

            migrationBuilder.DropTable(
                name: "DeputyVotes");

            migrationBuilder.DropTable(
                name: "ImportQuarantine");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VoteFactionStats");

            migrationBuilder.DropTable(
                name: "Deputies");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "FactionPeriods");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "Convocations");
        }
    }
}