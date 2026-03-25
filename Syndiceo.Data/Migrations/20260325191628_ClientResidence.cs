using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndiceo.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClientResidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Apartments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_UserId",
                table: "Apartments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apartments_AspNetUsers_UserId",
                table: "Apartments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apartments_AspNetUsers_UserId",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_UserId",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Apartments");
        }
    }
}
