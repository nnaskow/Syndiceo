using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndiceo.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTotalSumForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__TotalSum__Entran__756D6ECB",
                table: "TotalSum");

            migrationBuilder.AddForeignKey(
                name: "FK_TotalSum_Entrances",
                table: "TotalSum",
                column: "EntranceId",
                principalTable: "Entrances",
                principalColumn: "EntranceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TotalSum_Entrances",
                table: "TotalSum");

            migrationBuilder.AddForeignKey(
                name: "FK__TotalSum__Entran__756D6ECB",
                table: "TotalSum",
                column: "EntranceId",
                principalTable: "Blocks",
                principalColumn: "BlockID");
        }
    }
}
