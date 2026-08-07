using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalStaffRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalStaffEmail",
                table: "UniversityRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalStaffRespondedAt",
                table: "UniversityRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStaffResponseNotes",
                table: "UniversityRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalStaffSentAt",
                table: "UniversityRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalStaffEmail",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "ExternalStaffRespondedAt",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "ExternalStaffResponseNotes",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "ExternalStaffSentAt",
                table: "UniversityRequests");
        }
    }
}
