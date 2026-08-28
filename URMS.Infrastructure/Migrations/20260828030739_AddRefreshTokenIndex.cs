using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UniversityRequests_ConfirmationToken",
                table: "UniversityRequests",
                column: "ConfirmationToken");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityRequests_CreatedAt",
                table: "UniversityRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityRequests_Status",
                table: "UniversityRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UniversityRequests_ConfirmationToken",
                table: "UniversityRequests");

            migrationBuilder.DropIndex(
                name: "IX_UniversityRequests_CreatedAt",
                table: "UniversityRequests");

            migrationBuilder.DropIndex(
                name: "IX_UniversityRequests_Status",
                table: "UniversityRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");
        }
    }
}
