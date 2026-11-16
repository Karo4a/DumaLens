using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

#nullable disable

namespace DumaLens.Context.Migrations
{
    /// <inheritdoc />
    public partial class IntKeysDeputyVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_Deputies_DeputyId",
                table: "DeputyVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_FactionPeriods_FactionPeriodId",
                table: "DeputyVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_Votes_VoteId",
                table: "DeputyVotes");

            // Узкие ключи справочников: identity заполняет существующие строки последовательностью
            // в физическом порядке строк (после UPDATE он может отличаться от порядка вставки).
            // Сидовые периоды получают фиксированные значения (ADR-14); чтобы они не столкнулись
            // со значениями архивных периодов, backfill сначала сдвигается за сидовый диапазон,
            // затем архивные периоды уплотняются в 57..N (ADR-14).
            // ВНИМАНИЕ: DeputyVotes пересоздаётся пустой — данные восстанавливаются полным импортом.
            migrationBuilder.AddColumn<int>(
                name: "RowId",
                table: "Votes",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "RowId",
                table: "Deputies",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<short>(
                name: "RowId",
                table: "FactionPeriods",
                type: "smallint",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            // Сдвиг всех значений backfill за пределы сидового диапазона 1..56:
            // физический порядок строк после UPDATE мог измениться, и архивный период
            // мог получить значение из 1..56 — без сдвига UpdateData сидов дал бы дубликат RowId.
            migrationBuilder.Sql("UPDATE \"FactionPeriods\" SET \"RowId\" = \"RowId\" + 1000;");

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("037fba2c-4821-4f0c-b118-191702aa102b"),
                column: "RowId",
                value: (short)53);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("03b2975f-0c6d-4117-ae9c-6437713e4cf7"),
                column: "RowId",
                value: (short)34);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("097aeacc-55c4-4c00-a595-daf65eed644b"),
                column: "RowId",
                value: (short)25);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("0a3c565b-ad02-447c-a9be-9eb10f70c354"),
                column: "RowId",
                value: (short)5);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("0d61b8d3-3374-49e9-8dec-fe9d887ee1e2"),
                column: "RowId",
                value: (short)45);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("13d93636-ebe0-48c1-8caf-a1644038f060"),
                column: "RowId",
                value: (short)52);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("1782447e-910d-42f9-ab92-cf47dfffd09e"),
                column: "RowId",
                value: (short)17);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("18edbd04-2265-457f-9e8b-0a17377a26aa"),
                column: "RowId",
                value: (short)37);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("1a15de28-511d-4807-9d47-361ae282559a"),
                column: "RowId",
                value: (short)20);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("2da0ebe0-a68b-4a57-9506-a766548db824"),
                column: "RowId",
                value: (short)56);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("2e323136-978d-4ae6-bded-ae56b10c95c4"),
                column: "RowId",
                value: (short)22);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("3064d9e6-d9fe-46b7-9878-466b1cb70f1d"),
                column: "RowId",
                value: (short)27);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("360aef12-a5e7-4876-8080-791474fe2683"),
                column: "RowId",
                value: (short)35);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("3d299bfd-86ab-40aa-a7a3-feb41ed35835"),
                column: "RowId",
                value: (short)44);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("3e57a969-8b4c-4bc0-b780-4164d8fdfafb"),
                column: "RowId",
                value: (short)47);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("3f1e4c94-4a1b-4b5c-9db6-0a095b12d960"),
                column: "RowId",
                value: (short)8);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("45fc0012-037f-4ef7-86b2-e5373b73e60e"),
                column: "RowId",
                value: (short)6);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("479075db-b7f5-459f-9009-588d946fb4d1"),
                column: "RowId",
                value: (short)50);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("525de271-d6b5-4cc6-97c9-68160fe063a6"),
                column: "RowId",
                value: (short)18);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("57b2cb7c-c086-4b0a-a651-1d25d2e8bf51"),
                column: "RowId",
                value: (short)54);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("64845a76-f80a-495c-9b7f-2d316b8b08e1"),
                column: "RowId",
                value: (short)32);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("6c55d2de-8184-4847-a937-7a5604375e98"),
                column: "RowId",
                value: (short)3);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("73aa83b0-4cbd-4d1d-bfd3-817b7584691f"),
                column: "RowId",
                value: (short)13);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("78fbf9e3-d315-4c9e-b71e-509a58ff7e13"),
                column: "RowId",
                value: (short)12);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("83b180c6-e062-4043-92fc-4e195fff8ae2"),
                column: "RowId",
                value: (short)46);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("92db6383-260b-42d5-b7d2-bb3d55fd3a87"),
                column: "RowId",
                value: (short)30);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("95596282-12fd-4831-b70d-2d04c15c5a95"),
                column: "RowId",
                value: (short)28);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("9c8ce4cd-cfaa-45b5-b78f-e1d403bcfeed"),
                column: "RowId",
                value: (short)24);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("9dc5165f-df1e-4574-85c5-b4f6f827500c"),
                column: "RowId",
                value: (short)4);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("9eca1ed4-0320-4122-8dff-06ecd774f433"),
                column: "RowId",
                value: (short)7);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("9fad73c0-322a-4b9a-813f-b33163b6f66b"),
                column: "RowId",
                value: (short)26);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("a583d916-1cca-4532-a7e0-a217b8e854e3"),
                column: "RowId",
                value: (short)29);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ac681593-4b27-4736-830b-0485a902f652"),
                column: "RowId",
                value: (short)38);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ad275bd5-0e8a-404e-91b8-bce6313aa5f4"),
                column: "RowId",
                value: (short)9);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("b12d0021-afb4-4d9c-afb4-8ecc1520be9b"),
                column: "RowId",
                value: (short)1);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("b3140166-5362-42fc-97cb-69766b9cf021"),
                column: "RowId",
                value: (short)49);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("b51c99bb-7967-4ea0-a9cf-7f9f2eefa604"),
                column: "RowId",
                value: (short)33);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("b562bf61-a5ea-4b29-807f-9f9fb68bfd5c"),
                column: "RowId",
                value: (short)10);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("b949ed05-f648-4847-b220-b24f5f2fbbf7"),
                column: "RowId",
                value: (short)16);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("bdf9bb0b-7949-437b-b984-1eac067eaef7"),
                column: "RowId",
                value: (short)39);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ca43b4b7-32eb-4041-af2c-ac5c3fb6fea1"),
                column: "RowId",
                value: (short)11);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ce2fc4c1-b08f-464b-a6db-afc84747133a"),
                column: "RowId",
                value: (short)36);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("de5885e3-2f98-40f7-a81a-5d59ab86c3ea"),
                column: "RowId",
                value: (short)21);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("de7587b9-4c9b-4841-83fb-0a0ca064c790"),
                column: "RowId",
                value: (short)48);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("df045fa7-b9cb-4edc-8e8f-b3632dc638c3"),
                column: "RowId",
                value: (short)55);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("e1c57b62-84e1-4ca4-b377-daa0379cb03f"),
                column: "RowId",
                value: (short)19);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("e326e66d-30cf-4e9d-a0ce-cff4c9b258c0"),
                column: "RowId",
                value: (short)31);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("e4d51638-bdeb-4587-8eef-7a9acb92f84a"),
                column: "RowId",
                value: (short)14);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("e6658cd3-6592-4872-b89c-548a00f7ed67"),
                column: "RowId",
                value: (short)43);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("e7125771-2a99-4ff2-9fd1-afc68c841c87"),
                column: "RowId",
                value: (short)2);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("eb123a42-74fa-46d6-9cd9-93b39bbfc5af"),
                column: "RowId",
                value: (short)23);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ee3550f1-a11b-4d54-b1f7-f2f763bf75c4"),
                column: "RowId",
                value: (short)15);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ee81b4a1-6a69-436d-a48b-6d7d328c5c2b"),
                column: "RowId",
                value: (short)40);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("fd3ba9c6-d7d9-4381-b955-80b7ab854d59"),
                column: "RowId",
                value: (short)42);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ff5fdbfd-f7c9-401e-a489-a4d844c49050"),
                column: "RowId",
                value: (short)51);

            migrationBuilder.UpdateData(
                table: "FactionPeriods",
                keyColumn: "Id",
                keyValue: new Guid("ff959403-b47e-4f60-bff9-a9d3e24f00d1"),
                column: "RowId",
                value: (short)41);

            // Архивные периоды (сдвинутые за 1000) уплотняются в 57..N детерминированно по CreatedAt, Id;
            // последовательность identity выравнивается по максимуму, чтобы новые периоды не столкнулись с сидами.
            migrationBuilder.Sql(
                "UPDATE \"FactionPeriods\" AS f SET \"RowId\" = 56 + r.rn " +
                "FROM (SELECT \"Id\", row_number() OVER (ORDER BY \"CreatedAt\", \"Id\") AS rn " +
                "FROM \"FactionPeriods\" WHERE \"RowId\" > 1000) AS r " +
                "WHERE f.\"Id\" = r.\"Id\";");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"FactionPeriods\"', 'RowId'), (SELECT MAX(\"RowId\") FROM \"FactionPeriods\"));");

            // Типы ключей несовместимы с uuid: потоковая таблица пересоздаётся и наполняется импортом заново
            migrationBuilder.DropTable(
                name: "DeputyVotes");

            migrationBuilder.CreateTable(
                name: "DeputyVotes",
                columns: table => new
                {
                    VoteId = table.Column<int>(type: "integer", nullable: false),
                    DeputyId = table.Column<int>(type: "integer", nullable: false),
                    FactionPeriodId = table.Column<short>(type: "smallint", nullable: true),
                    Result = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeputyVotes", x => new { x.VoteId, x.DeputyId });
                });

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

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Votes_RowId",
                table: "Votes",
                column: "RowId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FactionPeriods_RowId",
                table: "FactionPeriods",
                column: "RowId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Deputies_RowId",
                table: "Deputies",
                column: "RowId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_Deputies_DeputyId",
                table: "DeputyVotes",
                column: "DeputyId",
                principalTable: "Deputies",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_FactionPeriods_FactionPeriodId",
                table: "DeputyVotes",
                column: "FactionPeriodId",
                principalTable: "FactionPeriods",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_Votes_VoteId",
                table: "DeputyVotes",
                column: "VoteId",
                principalTable: "Votes",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_Deputies_DeputyId",
                table: "DeputyVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_FactionPeriods_FactionPeriodId",
                table: "DeputyVotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeputyVotes_Votes_VoteId",
                table: "DeputyVotes");

            migrationBuilder.DropTable(
                name: "DeputyVotes");

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
                });

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

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_Deputies_DeputyId",
                table: "DeputyVotes",
                column: "DeputyId",
                principalTable: "Deputies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_FactionPeriods_FactionPeriodId",
                table: "DeputyVotes",
                column: "FactionPeriodId",
                principalTable: "FactionPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeputyVotes_Votes_VoteId",
                table: "DeputyVotes",
                column: "VoteId",
                principalTable: "Votes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Votes_RowId",
                table: "Votes");

            migrationBuilder.DropIndex(
                name: "IX_Vote_RowId",
                table: "Votes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FactionPeriods_RowId",
                table: "FactionPeriods");

            migrationBuilder.DropIndex(
                name: "IX_FactionPeriod_RowId",
                table: "FactionPeriods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Deputies_RowId",
                table: "Deputies");

            migrationBuilder.DropIndex(
                name: "IX_Deputy_RowId",
                table: "Deputies");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Votes");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "FactionPeriods");

            migrationBuilder.DropColumn(
                name: "RowId",
                table: "Deputies");
        }
    }
}