using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndiceo.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReportsTableBcDmb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Report_AspNetUsers_UserId",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "FK_Report_Entrances_EntranceId",
                table: "Report");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Report",
                table: "Report");

            migrationBuilder.RenameTable(
                name: "Report",
                newName: "Reports");

            migrationBuilder.RenameIndex(
                name: "IX_Report_UserId",
                table: "Reports",
                newName: "IX_Reports_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Report_EntranceId",
                table: "Reports",
                newName: "IX_Reports_EntranceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reports",
                table: "Reports",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Entrances_EntranceId",
                table: "Reports",
                column: "EntranceId",
                principalTable: "Entrances",
                principalColumn: "EntranceID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_UserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Entrances_EntranceId",
                table: "Reports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reports",
                table: "Reports");

            migrationBuilder.RenameTable(
                name: "Reports",
                newName: "Report");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_UserId",
                table: "Report",
                newName: "IX_Report_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_EntranceId",
                table: "Report",
                newName: "IX_Report_EntranceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Report",
                table: "Report",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Report_AspNetUsers_UserId",
                table: "Report",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Report_Entrances_EntranceId",
                table: "Report",
                column: "EntranceId",
                principalTable: "Entrances",
                principalColumn: "EntranceID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
