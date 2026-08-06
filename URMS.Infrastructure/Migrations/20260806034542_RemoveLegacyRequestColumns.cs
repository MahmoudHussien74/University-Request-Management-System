using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRequestColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GPA",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "IsGpaConfirmedByAdvisor",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "RequestedHours",
                table: "UniversityRequests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UniversityRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GPA",
                table: "UniversityRequests",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGpaConfirmedByAdvisor",
                table: "UniversityRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "UniversityRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedHours",
                table: "UniversityRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UniversityRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
