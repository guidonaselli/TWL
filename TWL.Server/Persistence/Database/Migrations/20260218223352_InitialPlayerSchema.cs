using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TWL.Server.Persistence.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlayerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // accounts table is managed by legacy DbService — only create if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS accounts (
                    user_id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    pass_hash VARCHAR(128) NOT NULL
                );
            ");

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hp = table.Column<int>(type: "integer", nullable: false),
                    sp = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    rebirth_level = table.Column<int>(type: "integer", nullable: false),
                    exp = table.Column<int>(type: "integer", nullable: false),
                    exp_to_next_level = table.Column<int>(type: "integer", nullable: false),
                    stat_points = table.Column<int>(type: "integer", nullable: false),
                    str = table.Column<int>(type: "integer", nullable: false),
                    con = table.Column<int>(type: "integer", nullable: false),
                    @int = table.Column<int>(name: "int", type: "integer", nullable: false),
                    wis = table.Column<int>(type: "integer", nullable: false),
                    agi = table.Column<int>(type: "integer", nullable: false),
                    element = table.Column<int>(type: "integer", nullable: false),
                    gold = table.Column<int>(type: "integer", nullable: false),
                    premium_currency = table.Column<long>(type: "bigint", nullable: false),
                    daily_gift_accumulator = table.Column<long>(type: "bigint", nullable: false),
                    last_gift_reset_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    map_id = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<float>(type: "real", nullable: false),
                    y = table.Column<float>(type: "real", nullable: false),
                    active_pet_instance_id = table.Column<string>(type: "text", nullable: true),
                    inventory_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    equipment_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    bank_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    pets_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    skills_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    world_flags_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    processed_orders_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    instance_lockouts_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    quest_states_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    quest_progress_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    quest_flags_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    quest_completion_times_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    quest_start_times_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    last_saved = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_players_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_username",
                table: "accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_user_id",
                table: "players",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "players");

            // Do not drop accounts table — managed by legacy DbService
        }
    }
}
