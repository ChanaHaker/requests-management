using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestsManagement.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSortIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Requests_Title",
                table: "Requests",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_UpdatedAt",
                table: "Requests",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Title",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_UpdatedAt",
                table: "Requests");
        }
    }
}
