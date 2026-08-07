using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    public partial class RenameExternalStaffToExternalAdministration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing FK and index for StaffId
            migrationBuilder.DropForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_StaffId",
                table: "UniversityRequests");

            migrationBuilder.DropIndex(
                name: "IX_UniversityRequests_StaffId",
                table: "UniversityRequests");

            // Rename columns
            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "UniversityRequests",
                newName: "AdministrationId");

            migrationBuilder.RenameColumn(
                name: "StaffConfirmedAt",
                table: "UniversityRequests",
                newName: "AdministrationConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffEmail",
                table: "UniversityRequests",
                newName: "ExternalAdministrationEmail");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffSentAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffRespondedAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationRespondedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffResponseNotes",
                table: "UniversityRequests",
                newName: "ExternalAdministrationResponseNotes");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpSentAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpExpiresAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpExpiresAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpCodeHash",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpCodeHash");

            // Recreate index and FK for AdministrationId
            migrationBuilder.CreateIndex(
                name: "IX_UniversityRequests_AdministrationId",
                table: "UniversityRequests",
                column: "AdministrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_AdministrationId",
                table: "UniversityRequests",
                column: "AdministrationId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new FK and index
            migrationBuilder.DropForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_AdministrationId",
                table: "UniversityRequests");

            migrationBuilder.DropIndex(
                name: "IX_UniversityRequests_AdministrationId",
                table: "UniversityRequests");

            // Rename columns back
            migrationBuilder.RenameColumn(
                name: "AdministrationId",
                table: "UniversityRequests",
                newName: "StaffId");

            migrationBuilder.RenameColumn(
                name: "AdministrationConfirmedAt",
                table: "UniversityRequests",
                newName: "StaffConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationEmail",
                table: "UniversityRequests",
                newName: "ExternalStaffEmail");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationSentAt",
                table: "UniversityRequests",
                newName: "ExternalStaffSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationRespondedAt",
                table: "UniversityRequests",
                newName: "ExternalStaffRespondedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationResponseNotes",
                table: "UniversityRequests",
                newName: "ExternalStaffResponseNotes");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpSentAt",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpExpiresAt",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpExpiresAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpCodeHash",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpCodeHash");

            // Recreate index and FK for StaffId
            migrationBuilder.CreateIndex(
                name: "IX_UniversityRequests_StaffId",
                table: "UniversityRequests",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_StaffId",
                table: "UniversityRequests",
                column: "StaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
