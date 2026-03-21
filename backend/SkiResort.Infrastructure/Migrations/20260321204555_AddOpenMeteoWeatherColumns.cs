using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkiResort.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenMeteoWeatherColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApparentTemperatureCelsius",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CloudCoverPercent",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecipitationMm",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RainMm",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RelativeHumidityPercent",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SurfacePressureHpa",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureCelsius",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityMeters",
                table: "SnowConditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherCode",
                table: "SnowConditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WindDirectionDeg",
                table: "SnowConditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WindGustsKmh",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WindSpeedKmh",
                table: "SnowConditions",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApparentTemperatureCelsius",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "CloudCoverPercent",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "PrecipitationMm",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "RainMm",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "RelativeHumidityPercent",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "SurfacePressureHpa",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "TemperatureCelsius",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "VisibilityMeters",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "WeatherCode",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "WindDirectionDeg",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "WindGustsKmh",
                table: "SnowConditions");

            migrationBuilder.DropColumn(
                name: "WindSpeedKmh",
                table: "SnowConditions");
        }
    }
}
