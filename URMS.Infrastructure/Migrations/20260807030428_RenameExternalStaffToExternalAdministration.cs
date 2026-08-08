using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameExternalStaffToExternalAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_StaffId",
                table: "UniversityRequests");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "UniversityRequests",
                newName: "AdministrationId");

            migrationBuilder.RenameColumn(
                name: "StaffConfirmedAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffSentAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationRespondedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffResponseNotes",
                table: "UniversityRequests",
                newName: "ExternalAdministrationResponseNotes");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffRespondedAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpSentAt",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpExpiresAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpExpiresAt",
                table: "UniversityRequests",
                newName: "AdministrationConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffOtpCodeHash",
                table: "UniversityRequests",
                newName: "ExternalAdministrationOtpCodeHash");

            migrationBuilder.RenameColumn(
                name: "ExternalStaffEmail",
                table: "UniversityRequests",
                newName: "ExternalAdministrationEmail");

            migrationBuilder.RenameIndex(
                name: "IX_UniversityRequests_StaffId",
                table: "UniversityRequests",
                newName: "IX_UniversityRequests_AdministrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_AdministrationId",
                table: "UniversityRequests",
                column: "AdministrationId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UniversityRequests_AspNetUsers_AdministrationId",
                table: "UniversityRequests");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationSentAt",
                table: "UniversityRequests",
                newName: "StaffConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationResponseNotes",
                table: "UniversityRequests",
                newName: "ExternalStaffResponseNotes");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationRespondedAt",
                table: "UniversityRequests",
                newName: "ExternalStaffSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpSentAt",
                table: "UniversityRequests",
                newName: "ExternalStaffRespondedAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpExpiresAt",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpSentAt");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationOtpCodeHash",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpCodeHash");

            migrationBuilder.RenameColumn(
                name: "ExternalAdministrationEmail",
                table: "UniversityRequests",
                newName: "ExternalStaffEmail");

            migrationBuilder.RenameColumn(
                name: "AdministrationId",
                table: "UniversityRequests",
                newName: "StaffId");

            migrationBuilder.RenameColumn(
                name: "AdministrationConfirmedAt",
                table: "UniversityRequests",
                newName: "ExternalStaffOtpExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_UniversityRequests_AdministrationId",
                table: "UniversityRequests",
                newName: "IX_UniversityRequests_StaffId");

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
