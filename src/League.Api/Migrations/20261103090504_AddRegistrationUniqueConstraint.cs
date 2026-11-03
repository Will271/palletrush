using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace League.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registrations_heat_id",
                table: "registrations");

            migrationBuilder.CreateIndex(
                name: "ux_registrations_heat_driver",
                table: "registrations",
                columns: new[] { "heat_id", "driver_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_registrations_heat_driver",
                table: "registrations");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_heat_id",
                table: "registrations",
                column: "heat_id");
        }
    }
}
