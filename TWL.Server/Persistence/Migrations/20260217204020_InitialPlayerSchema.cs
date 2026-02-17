using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TWL.Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlayerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Hp = table.Column<int>(type: "integer", nullable: false),
                    Sp = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    RebirthLevel = table.Column<int>(type: "integer", nullable: false),
                    Exp = table.Column<int>(type: "integer", nullable: false),
                    ExpToNextLevel = table.Column<int>(type: "integer", nullable: false),
                    StatPoints = table.Column<int>(type: "integer", nullable: false),
                    Str = table.Column<int>(type: "integer", nullable: false),
                    Con = table.Column<int>(type: "integer", nullable: false),
                    Int = table.Column<int>(type: "integer", nullable: false),
                    Wis = table.Column<int>(type: "integer", nullable: false),
                    Agi = table.Column<int>(type: "integer", nullable: false),
                    Element = table.Column<int>(type: "integer", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    PremiumCurrency = table.Column<long>(type: "bigint", nullable: false),
                    DailyGiftAccumulator = table.Column<long>(type: "bigint", nullable: false),
                    LastGiftResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MapId = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    LastSaved = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
