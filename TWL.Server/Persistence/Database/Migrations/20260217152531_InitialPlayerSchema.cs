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
            // accounts table is assumed to exist (created by legacy DbService)

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pos_x = table.Column<float>(type: "real", nullable: false),
                    pos_y = table.Column<float>(type: "real", nullable: false),
                    map_id = table.Column<int>(type: "integer", nullable: false),
                    hp = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false)
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

            // IX_accounts_username is assumed to exist

            migrationBuilder.CreateIndex(
                name: "IX_players_user_id",
                table: "players",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "players");

            // Do not drop accounts table
        }
    }
}
