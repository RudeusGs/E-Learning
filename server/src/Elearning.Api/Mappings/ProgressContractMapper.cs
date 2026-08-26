using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Progress.Responses;
using Elearning.Application.Common;
using Elearning.Application.Progress;

namespace Elearning.Api.Mappings;

public static class ProgressContractMapper
{
    public static CourseProgressSummaryResponse ToResponse(this CourseProgressSummaryDto progress) =>
        new(progress.CompletedLessons, progress.TotalLessons, progress.Percentage);

    public static LessonProgressResponse ToResponse(this LessonProgressDto progress) =>
        new(progress.Status, progress.StartedAtUtc, progress.CompletedAtUtc);

    public static CursorPageResponse<ProgressRowResponse> ToResponse(this CursorPage<ProgressRowDto> page) =>
        new(
            page.Items.Select(row => new ProgressRowResponse(
                row.StudentId,
                row.StudentName,
                row.CourseId,
                row.CourseTitle,
                row.CompletedLessons,
                row.TotalLessons,
                row.Percentage)).ToList(),
            page.NextCursor,
            page.HasMore);

    public static StudentCourseProgressResponse ToResponse(this StudentCourseProgressDto progress) =>
        new(
            progress.CourseId,
            progress.CompletedLessons,
            progress.TotalLessons,
            progress.Percentage,
            progress.Lessons.Select(lesson => new LessonProgressDetailResponse(
                lesson.LessonId,
                lesson.Title,
                lesson.Status,
                lesson.StartedAtUtc,
                lesson.CompletedAtUtc)).ToList());
}
