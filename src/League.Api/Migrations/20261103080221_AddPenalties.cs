using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace League.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPenalties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "penalties",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cone_id = table.Column<string>(type: "text", nullable: false),
                    seconds = table.Column<int>(type: "integer", nullable: false),
                    sensor_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penalties", x => x.id);
                    table.ForeignKey(
                        name: "FK_penalties_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pending_cone_hits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cone_id = table.Column<string>(type: "text", nullable: false),
                    sensor_ts = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_cone_hits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_penalties_run_id_cone_id",
                table: "penalties",
                columns: new[] { "run_id", "cone_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pending_cone_hits_run_id_cone_id",
                table: "pending_cone_hits",
                columns: new[] { "run_id", "cone_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "penalties");

            migrationBuilder.DropTable(
                name: "pending_cone_hits");
        }
    }
}
