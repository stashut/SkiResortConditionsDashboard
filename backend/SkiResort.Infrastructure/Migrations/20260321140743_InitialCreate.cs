using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkiResort.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiftStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResortId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiftStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    ElevationBaseMeters = table.Column<int>(type: "integer", nullable: false),
                    ElevationTopMeters = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resorts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RunStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResortId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnowConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResortId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnowDepthCm = table.Column<decimal>(type: "numeric", nullable: false),
                    NewSnowCm = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnowConditions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiftStatuses_ResortId_UpdatedAt",
                table: "LiftStatuses",
                columns: new[] { "ResortId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RunStatuses_ResortId_UpdatedAt",
                table: "RunStatuses",
                columns: new[] { "ResortId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SnowConditions_ResortId_ObservedAt",
                table: "SnowConditions",
                columns: new[] { "ResortId", "ObservedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiftStatuses");

            migrationBuilder.DropTable(
                name: "Resorts");

            migrationBuilder.DropTable(
                name: "RunStatuses");

            migrationBuilder.DropTable(
                name: "SnowConditions");
        }
    }
}
