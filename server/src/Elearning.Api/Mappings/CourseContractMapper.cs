using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Courses.Responses;
using Elearning.Application.Common;
using Elearning.Application.Courses;
using ApiCourseWriteRequest = Elearning.Api.Contracts.Courses.Requests.CourseWriteRequest;

namespace Elearning.Api.Mappings;

public static class CourseContractMapper
{
    public static Elearning.Application.Courses.CourseWriteRequest ToApplication(this ApiCourseWriteRequest request) =>
        new(request.Title, request.Description, request.ThumbnailUrl, request.Status, request.SortOrder, request.Version);

    public static AdminCourseDetailResponse ToResponse(this AdminCourseDetailDto course) =>
        new(
            course.Id,
            course.Title,
            course.Description,
            course.ThumbnailUrl,
            course.Status,
            course.SortOrder,
            course.Version);

    public static CursorPageResponse<AdminCourseListResponse> ToResponse(this CursorPage<AdminCourseListDto> page) =>
        new(page.Items.Select(course => new AdminCourseListResponse(
            course.Id,
            course.Title,
            course.Status,
            course.SortOrder,
            course.LessonCount,
            course.StudentCount,
            course.Version)).ToList(), page.NextCursor, page.HasMore);

    public static CursorPageResponse<StudentCourseResponse> ToResponse(this CursorPage<StudentCourseDto> page) =>
        new(
            page.Items.Select(course => new StudentCourseResponse(
                course.Id,
                course.Title,
                course.Description,
                course.ThumbnailUrl,
                course.TotalLessons,
                course.CompletedLessons,
                course.Percentage)).ToList(),
            page.NextCursor,
            page.HasMore);

    public static StudentCourseDetailResponse ToResponse(this StudentCourseDetailDto course) =>
        new(
            course.Id,
            course.Title,
            course.Description,
            course.CompletedLessons,
            course.TotalLessons,
            course.Percentage,
            course.Lessons.Select(lesson => new StudentLessonSummaryResponse(
                lesson.Id,
                lesson.Title,
                lesson.SortOrder,
                lesson.State,
                lesson.CanAccess)).ToList());
}
