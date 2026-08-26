using Elearning.Api.Contracts.Common;
using Elearning.Api.Contracts.Students.Responses;
using Elearning.Application.Common;
using Elearning.Application.Students;
using ApiStudentCreateRequest = Elearning.Api.Contracts.Students.Requests.StudentCreateRequest;
using ApiStudentUpdateRequest = Elearning.Api.Contracts.Students.Requests.StudentUpdateRequest;

namespace Elearning.Api.Mappings;

public static class StudentContractMapper
{
    public static StudentCreateRequest ToApplication(this ApiStudentCreateRequest request) =>
        new(request.FullName, request.Email, request.InitialPassword);

    public static StudentUpdateRequest ToApplication(this ApiStudentUpdateRequest request) =>
        new(request.FullName);

    public static StudentDetailResponse ToResponse(this StudentDetailDto student) =>
        new(
            student.Id,
            student.FullName,
            student.Email,
            student.Status,
            student.Enrollments.Select(enrollment => new StudentEnrollmentResponse(
                enrollment.Id,
                enrollment.CourseId,
                enrollment.CourseTitle,
                enrollment.Status)).ToList());

    public static CursorPageResponse<StudentListItemResponse> ToResponse(this CursorPage<StudentListItemDto> page) =>
        new(
            page.Items.Select(student => new StudentListItemResponse(
                student.Id,
                student.FullName,
                student.Email,
                student.CourseCount,
                student.Status)).ToList(),
            page.NextCursor,
            page.HasMore);
}
