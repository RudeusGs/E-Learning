using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF generates constant arrays for migration operations

namespace Elearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Status",
                table: "Lessons",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_Status",
                table: "LessonProgress",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Status_Id",
                table: "Enrollments",
                columns: new[] { "Status", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email_Trgm",
                table: "AspNetUsers",
                column: "Email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FullName",
                table: "AspNetUsers",
                column: "FullName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Status",
                table: "AspNetUsers",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_Status",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgress_Status",
                table: "LessonProgress");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_Status_Id",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email_Trgm",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FullName",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Status",
                table: "AspNetUsers");
        }
    }
}
