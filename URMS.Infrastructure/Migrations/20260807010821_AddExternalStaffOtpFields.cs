using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalStaffOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalStaffOtpCodeHash",
                table: "UniversityRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalStaffOtpExpiresAt",
                table: "UniversityRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalStaffOtpSentAt",
                table: "UniversityRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalStaffOtpCodeHash",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "ExternalStaffOtpExpiresAt",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "ExternalStaffOtpSentAt",
                table: "UniversityRequests");
        }
    }
}
