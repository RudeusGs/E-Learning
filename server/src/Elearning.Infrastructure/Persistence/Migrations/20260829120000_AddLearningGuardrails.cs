using Elearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elearning.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ElearningDbContext))]
    [Migration("20260829120000_AddLearningGuardrails")]
    public partial class AddLearningGuardrails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoDurationSeconds",
                table: "Lessons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Placement",
                table: "Questions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Reinforcement");

            migrationBuilder.AddColumn<int>(
                name: "VideoTimestampSeconds",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoMaxPositionSeconds",
                table: "LessonProgress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VideoLastPositionSeconds",
                table: "LessonProgress",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VideoHeartbeatAtUtc",
                table: "LessonProgress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VideoCompletedAtUtc",
                table: "LessonProgress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lessons_VideoDuration",
                table: "Lessons",
                sql: "\"VideoDurationSeconds\" IS NULL OR \"VideoDurationSeconds\" BETWEEN 1 AND 43200");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Placement",
                table: "Questions",
                sql: "\"Placement\" IN ('Reinforcement', 'VideoCheckpoint')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_VideoTimestamp",
                table: "Questions",
                sql: "(\"Placement\" = 'Reinforcement' AND \"VideoTimestampSeconds\" IS NULL) OR (\"Placement\" = 'VideoCheckpoint' AND \"VideoTimestampSeconds\" >= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonProgress_VideoPosition",
                table: "LessonProgress",
                sql: "\"VideoMaxPositionSeconds\" >= 0 AND (\"VideoLastPositionSeconds\" IS NULL OR \"VideoLastPositionSeconds\" >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LessonId_Placement_VideoTimestampSeconds",
                table: "Questions",
                columns: ["LessonId", "Placement", "VideoTimestampSeconds"]);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_StudentId_QuestionId_IsCorrect",
                table: "StudentAnswers",
                columns: ["StudentId", "QuestionId", "IsCorrect"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_LessonId_Placement_VideoTimestampSeconds",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_StudentAnswers_StudentId_QuestionId_IsCorrect",
                table: "StudentAnswers");

            migrationBuilder.DropCheckConstraint(name: "CK_Lessons_VideoDuration", table: "Lessons");
            migrationBuilder.DropCheckConstraint(name: "CK_Questions_Placement", table: "Questions");
            migrationBuilder.DropCheckConstraint(name: "CK_Questions_VideoTimestamp", table: "Questions");
            migrationBuilder.DropCheckConstraint(name: "CK_LessonProgress_VideoPosition", table: "LessonProgress");

            migrationBuilder.DropColumn(name: "VideoDurationSeconds", table: "Lessons");
            migrationBuilder.DropColumn(name: "Placement", table: "Questions");
            migrationBuilder.DropColumn(name: "VideoTimestampSeconds", table: "Questions");
            migrationBuilder.DropColumn(name: "VideoMaxPositionSeconds", table: "LessonProgress");
            migrationBuilder.DropColumn(name: "VideoLastPositionSeconds", table: "LessonProgress");
            migrationBuilder.DropColumn(name: "VideoHeartbeatAtUtc", table: "LessonProgress");
            migrationBuilder.DropColumn(name: "VideoCompletedAtUtc", table: "LessonProgress");
        }
    }
}
