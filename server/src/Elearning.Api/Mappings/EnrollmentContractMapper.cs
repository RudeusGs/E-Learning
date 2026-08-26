using Elearning.Api.Contracts.Enrollments.Responses;
using Elearning.Application.Enrollments;
using ApiEnrollmentRequest = Elearning.Api.Contracts.Enrollments.Requests.EnrollmentRequest;

namespace Elearning.Api.Mappings;

public static class EnrollmentContractMapper
{
    public static EnrollmentRequest ToApplication(this ApiEnrollmentRequest request) =>
        new(request.StudentId, request.CourseId);

    public static EnrollmentResponse ToResponse(this EnrollmentDto enrollment) =>
        new(enrollment.Id, enrollment.StudentId, enrollment.CourseId, enrollment.Status);
}
