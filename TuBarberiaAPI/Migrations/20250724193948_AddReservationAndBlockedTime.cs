using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuBarberiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationAndBlockedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarberBlockedTimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarberBlockedTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarberBlockedTimes_Users_BarberId",
                        column: x => x.BarberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarberId = table.Column<int>(type: "int", nullable: false),
                    BarberServiceId = table.Column<int>(type: "int", nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_BarberServices_BarberServiceId",
                        column: x => x.BarberServiceId,
                        principalTable: "BarberServices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reservations_Users_BarberId",
                        column: x => x.BarberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarberBlockedTimes_BarberId",
                table: "BarberBlockedTimes",
                column: "BarberId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BarberId",
                table: "Reservations",
                column: "BarberId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BarberServiceId",
                table: "Reservations",
                column: "BarberServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarberBlockedTimes");

            migrationBuilder.DropTable(
                name: "Reservations");
        }
    }
}
