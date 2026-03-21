using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkiResort.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResortCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LatitudeDeg",
                table: "Resorts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudeDeg",
                table: "Resorts",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatitudeDeg",
                table: "Resorts");

            migrationBuilder.DropColumn(
                name: "LongitudeDeg",
                table: "Resorts");
        }
    }
}
