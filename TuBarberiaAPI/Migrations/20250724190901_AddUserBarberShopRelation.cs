using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuBarberiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBarberShopRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BarberShops_Users_AdminUserId",
                table: "BarberShops");

            migrationBuilder.AddColumn<int>(
                name: "BarberShopId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "BarberShops",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BarberShopId",
                table: "Users",
                column: "BarberShopId");

            migrationBuilder.CreateIndex(
                name: "IX_BarberShops_UserId",
                table: "BarberShops",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BarberShops_Users_AdminUserId",
                table: "BarberShops",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BarberShops_Users_UserId",
                table: "BarberShops",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_BarberShops_BarberShopId",
                table: "Users",
                column: "BarberShopId",
                principalTable: "BarberShops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BarberShops_Users_AdminUserId",
                table: "BarberShops");

            migrationBuilder.DropForeignKey(
                name: "FK_BarberShops_Users_UserId",
                table: "BarberShops");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_BarberShops_BarberShopId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BarberShopId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_BarberShops_UserId",
                table: "BarberShops");

            migrationBuilder.DropColumn(
                name: "BarberShopId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BarberShops");

            migrationBuilder.AddForeignKey(
                name: "FK_BarberShops_Users_AdminUserId",
                table: "BarberShops",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
