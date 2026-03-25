using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syndiceo.Data.Migrations
{
    /// <inheritdoc />
    public partial class LastNameAddDropAddressDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AddressDetails",
                table: "AspNetUsers",
                newName: "LastName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "AspNetUsers",
                newName: "AddressDetails");
        }
    }
}
