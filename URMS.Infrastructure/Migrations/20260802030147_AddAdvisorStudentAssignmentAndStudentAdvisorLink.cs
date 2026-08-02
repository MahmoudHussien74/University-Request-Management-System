using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvisorStudentAssignmentAndStudentAdvisorLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicAdvisorId",
                table: "Students",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdvisorStudentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniversityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdvisorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorStudentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvisorStudentAssignments_AspNetUsers_AdvisorId",
                        column: x => x.AdvisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_AcademicAdvisorId",
                table: "Students",
                column: "AcademicAdvisorId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorStudentAssignments_AdvisorId",
                table: "AdvisorStudentAssignments",
                column: "AdvisorId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorStudentAssignments_UniversityCode",
                table: "AdvisorStudentAssignments",
                column: "UniversityCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AspNetUsers_AcademicAdvisorId",
                table: "Students",
                column: "AcademicAdvisorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_AspNetUsers_AcademicAdvisorId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "AdvisorStudentAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Students_AcademicAdvisorId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AcademicAdvisorId",
                table: "Students");
        }
    }
}
